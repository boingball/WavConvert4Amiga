using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WavConvert4Amiga
{
    public class AudioEffectsProcessor
    {
        // Apply underwater effect (low-pass filter with resonance)
        public byte[] ApplyUnderwaterEffect(byte[] input, int sampleRate)
        {
            float resonance = 2.0f;
            float cutoffFrequency = sampleRate * 0.15f; // Lower cutoff for underwater sound
            float alpha = cutoffFrequency / (cutoffFrequency + sampleRate);

            float[] filteredSamples = new float[input.Length];
            float lastValue = (input[0] - 128) / 128.0f;
            float lastLastValue = lastValue;

            for (int i = 0; i < input.Length; i++)
            {
                float currentSample = (input[i] - 128) / 128.0f;
                float resonanceFeedback = resonance * (lastValue - lastLastValue);
                lastLastValue = lastValue;
                lastValue = lastValue + alpha * (currentSample - lastValue + resonanceFeedback);
                filteredSamples[i] = lastValue;
            }

            // Convert back to PCM bytes with slight amplitude boost
            return ConvertToBytes(filteredSamples, 1.2f);
        }

        // Apply robot voice effect (ring modulation)
        public byte[] ApplyRobotEffect(byte[] input, int sampleRate)
        {
            float modulationFrequency = 50.0f; // Frequency for robot sound
            float[] samples = new float[input.Length];

            for (int i = 0; i < input.Length; i++)
            {
                float time = (float)i / sampleRate;
                float modulator = (float)Math.Sin(2 * Math.PI * modulationFrequency * time);
                float sample = (input[i] - 128) / 128.0f;
                samples[i] = sample * modulator;
            }

            return ConvertToBytes(samples, 1.5f);
        }

        // Apply pitch shift effect (simple implementation)
        public byte[] ApplyPitchShift(byte[] input, int sampleRate, float pitchFactor)
        {
            int outputLength = (int)(input.Length / pitchFactor);
            float[] output = new float[outputLength];

            for (int i = 0; i < outputLength; i++)
            {
                float position = i * pitchFactor;
                int index = (int)position;
                if (index < input.Length - 1)
                {
                    float fraction = position - index;
                    float sample1 = (input[index] - 128) / 128.0f;
                    float sample2 = (input[index + 1] - 128) / 128.0f;
                    output[i] = sample1 + (sample2 - sample1) * fraction;
                }
            }

            return ConvertToBytes(output, 1.0f);
        }

        // Apply echo effect
        public byte[] ApplyEchoEffect(byte[] input, int sampleRate)
        {
            int delayMs = 100; // Echo delay in milliseconds
            float decay = 0.5f; // Echo decay factor
            int delaySamples = (int)(sampleRate * (delayMs / 1000.0f));
            float[] output = new float[input.Length];

            // Convert input to float samples
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = (input[i] - 128) / 128.0f;
                if (i >= delaySamples)
                {
                    output[i] += output[i - delaySamples] * decay;
                }
            }

            return ConvertToBytes(output, 0.8f);
        }

        // Helper method to convert float samples back to bytes
        private byte[] ConvertToBytes(float[] samples, float amplification = 1.0f)
        {
            byte[] output = new byte[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                float sample = samples[i] * amplification;
                sample = Math.Max(-1.0f, Math.Min(1.0f, sample));
                output[i] = (byte)Math.Max(0, Math.Min(255, (sample * 128.0f) + 128));
            }
            return output;
        }
    }
}