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
            float lastValue = (input[0] - 128) / 128.0f; // Convert to normalized float (-1 to 1)

            for (int i = 0; i < input.Length; i++)
            {
                float currentSample = (input[i] - 128) / 128.0f; // Convert to normalized float
                lastValue = lastValue + alpha * (currentSample - lastValue); // Apply filter
                filteredSamples[i] = lastValue; // Store filtered value
            }

            // Convert filteredSamples back to PCM bytes
            byte[] output = new byte[filteredSamples.Length];
            for (int i = 0; i < filteredSamples.Length; i++)
            {
                output[i] = (byte)Math.Max(0, Math.Min(255, (filteredSamples[i] * 128.0f) + 128));
            }

            return output;
        }

        public byte[] ApplyAmplification(byte[] input, float factor)
        {
            if (factor == 1.0f) return input;

            byte[] output = new byte[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                float sample = (input[i] - 128) / 128.0f;
                sample *= factor;
                sample = Math.Max(-1.0f, Math.Min(1.0f, sample));
                output[i] = (byte)Math.Max(0, Math.Min(255, (sample * 128.0f) + 128));
            }

            return output;
        }

        public byte[] ResampleAudio(byte[] input, int targetSampleRate, WaveFormat originalFormat)
        {
            using (var memoryStream = new MemoryStream(input))
            using (var waveStream = new RawSourceWaveStream(memoryStream, originalFormat))
            using (var resampler = new MediaFoundationResampler(waveStream, new WaveFormat(targetSampleRate, originalFormat.BitsPerSample, originalFormat.Channels)))
            {
                resampler.ResamplerQuality = 60;
                return ReadFully(resampler);
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
