using NAudio.Wave;
using System.Collections.Generic;

namespace Symbl.Realtime.Audio.Helper
{
    /// <summary>
    /// Reused code 
    /// https://researchaholic.com/2013/08/01/split-a-pcm-stereo-into-multiple-mono-files-by-channel/
    /// </summary>
    public class WavFileHelper
    {
        public WavFileHelper()
        {

        }

        public void WriteNewAudioFiles(byte[] input,
            string filePath, int channels)
        {
            // split the stream into multiple audio files
            var outputs = new List<List<byte>>();
            for (int i = 0; i < channels; i++)
            {
                outputs.Add(new List<byte>());
            }

            int channelCount = channels;
            var count = 0;
            for (int i = 0; i < input.Length; i += 2)
            {
                outputs[count].Add(input[i]);
                outputs[count].Add(input[i + 1]);
                count++;
                channelCount--;
                if (channelCount >= 1) continue;
                channelCount = channels;
                count = 0;
            }

            // write each byte aray to a new mono file
            count = 0;
            foreach (var o in outputs)
            {
                byte[] data = o.ToArray();
                WaveFormat waveFormat1 = new WaveFormat(8000, 16, 1);
                using (WaveFileWriter writer333 = new WaveFileWriter(string.Format(@"{0}\mono{1}.wav", filePath, count), waveFormat1))
                {
                    writer333.Write(data, 0, data.Length);
                }
                count++;
            }
        }
    }
}
