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
        public List<(int start, int end)> CutRegions { get; private set; }
        public float AmplificationFactor { get; private set; }
        public List<string> AppliedEffects { get; private set; }

        public AudioState(byte[] audioData, int sampleRate, List<(int start, int end)> cutRegions = null,
                         float amplificationFactor = 1.0f, List<string> appliedEffects = null)
        {
            AudioData = new byte[audioData.Length];
            Array.Copy(audioData, AudioData, audioData.Length);
            SampleRate = sampleRate;
            CutRegions = cutRegions?.ToList() ?? new List<(int start, int end)>();
            AmplificationFactor = amplificationFactor;
            AppliedEffects = appliedEffects?.ToList() ?? new List<string>();
        }
    }
}
