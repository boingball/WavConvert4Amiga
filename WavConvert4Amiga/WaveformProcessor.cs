using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WavConvert4Amiga
{
    public class WaveformProcessor
    {
        public byte[] ApplyLowPassFilter(byte[] input, int sampleRate, float cutoffFrequency)
        {
            float alpha = cutoffFrequency / (cutoffFrequency + sampleRate);
            float[] filteredSamples = new float[input.Length];
            float lastValue = (input[0] - 128) / 128.0f;

            for (int i = 0; i < input.Length; i++)
            {
                float currentSample = (input[i] - 128) / 128.0f;
                lastValue = lastValue + alpha * (currentSample - lastValue);
                filteredSamples[i] = lastValue;
            }

            // Convert back to PCM bytes
            byte[] output = new byte[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = (byte)Math.Max(0, Math.Min(255, (filteredSamples[i] * 128.0f) + 128.0f));
            }

            return output;
        }

        public byte[] ApplyAmplification(byte[] input, float factor)
        {
            if (factor == 1.0f) return input;

            byte[] output = new byte[input.Length];

            // Process each sample
            for (int i = 0; i < input.Length; i++)
            {
                // Convert unsigned PCM (0-255) to signed float (-1.0 to 1.0)
                float sample = (input[i] - 128) / 128.0f;

                // Apply amplification
                sample *= factor;

                // Hard limiting to prevent clipping
                sample = Math.Max(-1.0f, Math.Min(1.0f, sample));

                // Convert back to unsigned PCM (0-255)
                output[i] = (byte)Math.Max(0, Math.Min(255, (sample * 128.0f) + 128.0f));
            }

            return output;
        }


        public byte[] ResampleAudio(byte[] input, int targetSampleRate, WaveFormat originalFormat)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var sourceMs = new MemoryStream(input))
                using (var reader = new RawSourceWaveStream(sourceMs, originalFormat))
                using (var resampler = new MediaFoundationResampler(reader, new WaveFormat(targetSampleRate, 8, 1)))
                {
                    resampler.ResamplerQuality = 60;
                    byte[] buffer = new byte[4096];
                    int read;
                    while ((read = resampler.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        memoryStream.Write(buffer, 0, read);
                    }
                }
                return memoryStream.ToArray();
            }
        }

        private byte[] ReadFully(IWaveProvider provider)
        {
            using (var memoryStream = new MemoryStream())
            {
                byte[] buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = provider.Read(buffer, 0, buffer.Length)) > 0)
                {
                    memoryStream.Write(buffer, 0, bytesRead);
                }
                return memoryStream.ToArray();
            }
        }
    }

}
