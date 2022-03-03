using System;
using System.Linq;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Symbl.Realtime.Audio
{
    /// <summary>
    /// Reused and Refactored code from https://gist.github.com/xamlmonkey/4737291
    /// </summary>
    public class SymblWebSocketWrapper
    {
        private const int ReceiveChunkSize = 1024;
        private const int SendChunkSize = 1024;

        private readonly Uri _uri;
        private readonly ClientWebSocket _webSocketClient;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _cancellationToken;

        private Action<SymblWebSocketWrapper> _onConnected;
        private Action<string, SymblWebSocketWrapper> _onMessage;
        private Action<SymblWebSocketWrapper> _onDisconnected;

        protected SymblWebSocketWrapper(string uri)
        {
            _webSocketClient = new ClientWebSocket();
            _webSocketClient.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
            _uri = new Uri(uri);
            _cancellationToken = _cancellationTokenSource.Token;
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="uri">The URI of the WebSocket server.</param>
        /// <returns></returns>
        public static SymblWebSocketWrapper Create(string uri)
        {
            return new SymblWebSocketWrapper(uri);
        }

        /// <summary>
        /// Connects to the WebSocket server.
        /// </summary>
        /// <returns></returns>
        public SymblWebSocketWrapper Connect()
        {
            _webSocketClient.ConnectAsync(_uri, _cancellationToken)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
            CallOnConnected();
            StartListen();
            return this;
        }

        public void Disconnect()
        {
            _webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure,
                string.Empty, CancellationToken.None)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
            CallOnDisconnected();
        }

        /// <summary>
        /// Set the Action to call when the connection has been established.
        /// </summary>
        /// <param name="onConnect">The Action to call.</param>
        /// <returns></returns>
        public SymblWebSocketWrapper OnConnect(Action<SymblWebSocketWrapper> onConnect)
        {
            _onConnected = onConnect;
            return this;
        }

        /// <summary>
        /// Set the Action to call when the connection has been terminated.
        /// </summary>
        /// <param name="onDisconnect">The Action to call</param>
        /// <returns></returns>
        public SymblWebSocketWrapper OnDisconnect(Action<SymblWebSocketWrapper> onDisconnect)
        {
            _onDisconnected = onDisconnect;
            return this;
        }

        /// <summary>
        /// Set the Action to call when a messages has been received.
        /// </summary>
        /// <param name="onMessage">The Action to call.</param>
        /// <returns></returns>
        public SymblWebSocketWrapper OnMessage(Action<string, SymblWebSocketWrapper> onMessage)
        {
            _onMessage = onMessage;
            return this;
        }

        /// <summary>
        /// Send a message to the WebSocket server.
        /// </summary>
        /// <param name="message">The message to send</param>
        public void SendMessage(string message)
        {
            SendMessageAsync(message)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Send Start Request
        /// </summary>
        public void SendStartRequest()
        {
            StartRequest startRequest = BuildStartRequest();
            string startRequestJson = JsonConvert.SerializeObject(startRequest);
            SendMessage(startRequestJson);
        }

        private StartRequest BuildStartRequest()
        {
            return new StartRequest
            {
                type = "start_request",
                meetingTitle = "Websockets How-to",
                insightTypes = new System.Collections.Generic.List<string>
                {
                    "question", "action_item"
                },
                config = new Config
                {
                    languageCode = "en-US",
                    speechRecognition = new SpeechRecognition
                    {
                        encoding = "LINEAR16",
                        sampleRateHertz = 16000
                    }
                },
                speaker = new Speaker
                {
                    name = "Demo",
                    userId = "example@symbl.ai"
                }
            };
        }

        public void SendMessage(byte[] bytes)
        {
            SendMessageAsync(bytes)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        private async Task SendMessageAsync(byte[] bytes)
        {
            if (_webSocketClient.State != WebSocketState.Open)
            {
                throw new Exception("Connection is not open.");
            }

            int offSet = 0;
            foreach(var chunkedBytes in Split(bytes, SendChunkSize))
            {
                if ((offSet + SendChunkSize) <= bytes.Length)
                {
                    await _webSocketClient.SendAsync(new ArraySegment<byte>(bytes, offSet, SendChunkSize),
                       WebSocketMessageType.Binary, true, _cancellationToken);
                    offSet += SendChunkSize;
                }
            }

            int lastBytes = bytes.Length - offSet;
            if(lastBytes > 0)
            {
                await _webSocketClient.SendAsync(new ArraySegment<byte>(bytes, offSet, lastBytes),
                       WebSocketMessageType.Binary, true, _cancellationToken);
            }
        }

        /// <summary>
        /// https://stackoverflow.com/questions/11816295/splitting-a-byte-into-multiple-byte-arrays-in-c-sharp
        /// </summary>
        /// <param name="value"></param>
        /// <param name="bufferLength"></param>
        /// <returns></returns>
        public static IEnumerable<byte[]> Split(byte[] value,
            int bufferLength)
        {
            int countOfArray = value.Length / bufferLength;
            if (value.Length % bufferLength > 0)
                countOfArray++;

            for (int i = 0; i < countOfArray; i++)
            {
                yield return value.Skip(i * bufferLength).Take(bufferLength)
                    .ToArray();
            }
        }

        private async Task SendMessageAsync(string message)
        {
            if (_webSocketClient.State != WebSocketState.Open)
            {
                throw new Exception("Connection is not open.");
            }

            var messageBuffer = Encoding.UTF8.GetBytes(message);
            var messagesCount = (int)Math.Ceiling((double)messageBuffer.Length / SendChunkSize);

            for (var i = 0; i < messagesCount; i++)
            {
                var offset = (SendChunkSize * i);
                var count = SendChunkSize;
                var lastMessage = ((i + 1) == messagesCount);

                if ((count * (i + 1)) > messageBuffer.Length)
                {
                    count = messageBuffer.Length - offset;
                }

                await _webSocketClient.SendAsync(new ArraySegment<byte>(messageBuffer,
                        offset, count), WebSocketMessageType.Text,
                        lastMessage, _cancellationToken);
            }
        }

        private async Task StartListen()
        {
            LogDebugInfo("StartListen");
            var buffer = new byte[ReceiveChunkSize];

            try
            {
                while (_webSocketClient.State == WebSocketState.Open)
                {
                    var stringResult = new StringBuilder();
                    WebSocketReceiveResult result;
                   
                    do
                    {
                        result = await _webSocketClient.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationToken);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await
                                _webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            CallOnDisconnected();
                        }
                        else
                        {
                            var str = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            stringResult.Append(str);
                            LogDebugInfo(str);
                        }
                    } while (!result.EndOfMessage);

                    CallOnMessage(stringResult);
                }
            }
            catch (Exception)
            {
                CallOnDisconnected();
            }
        }

        private void CallOnMessage(StringBuilder stringResult)
        {
            LogDebugInfo("CallOnMessage");
            if (_onMessage != null)
                RunInTask(() => _onMessage(stringResult.ToString(), this));
        }

        private void CallOnDisconnected()
        {
            LogDebugInfo("CallOnDisconnected");
            if (_onDisconnected != null)
                RunInTask(() => _onDisconnected(this));
        }

        private void CallOnConnected()
        {
            LogDebugInfo("CallOnConnected");
            if (_onConnected != null)
                RunInTask(() => _onConnected(this));
        }

        private static void RunInTask(Action action)
        {
            LogDebugInfo("RunInTask");
            Task.Factory.StartNew(action);
        }

        private static void LogDebugInfo(string message)
        {
            Debug.WriteLine(message);
            Console.WriteLine(message);
        }
    }
}
