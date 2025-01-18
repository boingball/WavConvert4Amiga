using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WavConvert4Amiga
{
    public class SampleRateConverter
    {
        private int originalSampleRate;
        private int currentSampleRate;
        private int loopStart = -1;
        private int loopEnd = -1;

        public void SetSampleRate(int newRate, ref byte[] audioData)
        {
            if (originalSampleRate == 0)
            {
                originalSampleRate = newRate;
                currentSampleRate = newRate;
                return;
            }

            // Store old loop point positions relative to total length
            double oldLoopStartRatio = loopStart >= 0 ? loopStart / (double)audioData.Length : -1;
            double oldLoopEndRatio = loopEnd >= 0 ? loopEnd / (double)audioData.Length : -1;

            // Convert audio data to new sample rate
            using (var sourceMs = new MemoryStream())
            {
                var sourceFormat = new WaveFormat(currentSampleRate, 8, 1);
                using (var writer = new WaveFileWriter(sourceMs, sourceFormat))
                {
                    writer.Write(audioData, 0, audioData.Length);
                    writer.Flush();
                    sourceMs.Position = 0;

                    using (var reader = new WaveFileReader(sourceMs))
                    using (var resampler = new MediaFoundationResampler(reader,
                        new WaveFormat(newRate, 8, 1)))
                    {
                        resampler.ResamplerQuality = 60;
                        using (var outStream = new MemoryStream())
                        {
                            byte[] buffer = new byte[4096];
                            int read;
                            while ((read = resampler.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                outStream.Write(buffer, 0, read);
                            }
                            audioData = outStream.ToArray();
                        }
                    }
                }
            }

            // Scale loop points to new sample rate
            if (oldLoopStartRatio >= 0)
                loopStart = (int)(oldLoopStartRatio * audioData.Length);
            if (oldLoopEndRatio >= 0)
                loopEnd = (int)(oldLoopEndRatio * audioData.Length);

            currentSampleRate = newRate;
        }

        public (int start, int end) GetScaledLoopPoints() => (loopStart, loopEnd);

        public void SetLoopPoints(int start, int end)
        {
            loopStart = start;
            loopEnd = end;
        }
    }
}
