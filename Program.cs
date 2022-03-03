using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Symbl.Realtime.Audio
{
    class Program
    {
        private static string exePath = "";
        private static string accessToken = "";
        private static SymblWebSocketWrapper symblWebSocketWrapper;

        static async Task Main(string[] args)
        {
            string audioFileName = "SpellingSequence_1";
            exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var authResponse = new SymblAuth().GetAuthToken();

            if (authResponse != null)
            {
                accessToken = authResponse.accessToken;
            }

            string uniqueMeetingId = Guid.NewGuid().ToString();
            string symblEndpoint = $"wss://api.symbl.ai/v1/realtime/insights/{uniqueMeetingId}?access_token={accessToken}";

            symblWebSocketWrapper = SymblWebSocketWrapper.Create(symblEndpoint);
            symblWebSocketWrapper.Connect();

            // Send the Start Request
            symblWebSocketWrapper.SendStartRequest();

            // Send Audio Bytes
            var audioFileBytes = Get16BitAudioInBytes(audioFileName, "wav");
            symblWebSocketWrapper.SendMessage(audioFileBytes);

            Console.WriteLine("Completed Sending Audio. Press any key to exit!");
            Console.ReadLine();
        }

        /// <summary>
        /// Get the 16bit Audio in Bytes
        /// </summary>
        /// <param name="fileName">FileName</param>
        /// <param name="extension">Extension</param>
        /// <returns>Byte Array</returns>
        private static byte[] Get16BitAudioInBytes(string fileName,
           string extension)
        {
            string fullPath = string.Format("{0}\\Uploads\\{1}.{2}",
                exePath, fileName, extension);

            if (File.Exists(fullPath))
            {
                using (var reader = new WaveFileReader(fullPath))
                {
                    var newFormat = new WaveFormat(16000, 16, 1);
                    using (var conversionStream = new WaveFormatConversionStream(newFormat, reader))
                    {
                        WaveFileWriter.CreateWaveFile("output.wav",
                            conversionStream);
                    }
                }

                var fileBytes = File.ReadAllBytes("output.wav");
                File.Delete("output.wav");
                return fileBytes;
            }
            return null;
        }
    }
}
