using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WavConvert4Amiga
{
    public class AudioState
    {
        public byte[] AudioData { get; private set; }
        public int SampleRate { get; private set; }

        public AudioState(byte[] audioData, int sampleRate)
        {
            AudioData = new byte[audioData.Length];
            Array.Copy(audioData, AudioData, audioData.Length);
            SampleRate = sampleRate;
        }

    }
}
