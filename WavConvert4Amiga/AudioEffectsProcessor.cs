using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WavConvert4Amiga
{
    public class AudioEffectsProcessor
    {

        private Action<string> setCursorCallback;

        public AudioEffectsProcessor(Action<string> setCursorCallback = null)
        {
            this.setCursorCallback = setCursorCallback;
        }

        private void SetBusyCursor()
        {
            setCursorCallback?.Invoke("busy");
        }

        private void SetNormalCursor()
        {
            setCursorCallback?.Invoke("normal");
        }

        // Apply underwater effect (low-pass filter with resonance)
        public byte[] ApplyUnderwaterEffect(byte[] input, int sampleRate)
        {
            try
            {
                SetBusyCursor();
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
            finally
            {
                SetNormalCursor();
            }
        }

        // Apply robot voice effect (ring modulation)
        public byte[] ApplyRobotEffect(byte[] input, int sampleRate)
        {
            try
            {
                SetBusyCursor();
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
            finally
            {
                SetNormalCursor();
            }
        }

        // Apply pitch shift effect (simple implementation)
        public byte[] ApplyPitchShift(byte[] input, int sampleRate, float pitchFactor)
        {
            try
            {
                SetBusyCursor();
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
            finally
            {
                SetNormalCursor();
            }
        }

        // Apply echo effect
        public byte[] ApplyEchoEffect(byte[] input, int sampleRate)
        {
            try
            {
                SetBusyCursor();
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
            finally
            {
                SetNormalCursor();
            }
        }
        private static readonly int[] vocalFreqs = { 200, 400, 800, 1600, 2400, 3200 }; // Key vocal frequencies

        // Apply vocal removal effect (using frequency-based approach for mono)
        public byte[] ApplyVocalRemoval(byte[] input, int sampleRate)
        {
            try
            {
                SetBusyCursor();
                // We'll use multiple narrow band-stop filters to target vocal frequencies more precisely
                // Convert input bytes to normalized float samples (-1 to 1)
                float[] samples = new float[input.Length];
                for (int i = 0; i < input.Length; i++)
                {
                    samples[i] = (input[i] - 128) / 128.0f;
                }

                float[] output = samples;

                // Apply multiple band-stop filters
                foreach (int centerFreq in vocalFreqs)
                {
                    // Design narrow band-stop filter for each frequency
                    double w0 = 2 * Math.PI * centerFreq / sampleRate;
                    double bandwidth = 0.5; // Narrow bandwidth for precision
                    double alpha = Math.Sin(w0) * Math.Sinh(Math.Log(2) / 2 * bandwidth * w0 / Math.Sin(w0));

                    // Filter coefficients for notch filter
                    double a0 = 1 + alpha;
                    double a1 = -2 * Math.Cos(w0);
                    double a2 = 1 - alpha;
                    double b0 = 1;
                    double b1 = -2 * Math.Cos(w0);
                    double b2 = 1;

                    // Normalize coefficients
                    b0 /= a0;
                    b1 /= a0;
                    b2 /= a0;
                    a1 /= a0;
                    a2 /= a0;

                    float[] x = new float[3]; // Input history
                    float[] y = new float[3]; // Output history
                    float[] temp = new float[output.Length];

                    // Apply filter
                    for (int i = 0; i < output.Length; i++)
                    {
                        x[2] = x[1];
                        x[1] = x[0];
                        x[0] = output[i];

                        y[2] = y[1];
                        y[1] = y[0];

                        // Apply the filter
                        y[0] = (float)(b0 * x[0] + b1 * x[1] + b2 * x[2] - a1 * y[1] - a2 * y[2]);
                        temp[i] = y[0];
                    }

                    output = temp;
                }

                // Apply low-shelf filter to boost bass frequencies
                double shelfFreq = 120.0;
                double shelfGain = 2.0; // Boost bass
                double ws = 2 * Math.PI * shelfFreq / sampleRate;
                double S = 1.0;
                double A = Math.Pow(10, shelfGain / 40);
                double alpha_shelf = Math.Sin(ws) / 2 * Math.Sqrt((A + 1 / A) * (1 / S - 1) + 2);

                // Low-shelf coefficients
                double b0_shelf = A * ((A + 1) - (A - 1) * Math.Cos(ws) + 2 * Math.Sqrt(A) * alpha_shelf);
                double b1_shelf = 2 * A * ((A - 1) - (A + 1) * Math.Cos(ws));
                double b2_shelf = A * ((A + 1) - (A - 1) * Math.Cos(ws) - 2 * Math.Sqrt(A) * alpha_shelf);
                double a0_shelf = (A + 1) + (A - 1) * Math.Cos(ws) + 2 * Math.Sqrt(A) * alpha_shelf;
                double a1_shelf = -2 * ((A - 1) + (A + 1) * Math.Cos(ws));
                double a2_shelf = (A + 1) + (A - 1) * Math.Cos(ws) - 2 * Math.Sqrt(A) * alpha_shelf;

                // Normalize coefficients
                b0_shelf /= a0_shelf;
                b1_shelf /= a0_shelf;
                b2_shelf /= a0_shelf;
                a1_shelf /= a0_shelf;
                a2_shelf /= a0_shelf;

                float[] x_shelf = new float[3];
                float[] y_shelf = new float[3];
                float[] enhanced = new float[output.Length];

                // Apply low-shelf filter
                for (int i = 0; i < output.Length; i++)
                {
                    x_shelf[2] = x_shelf[1];
                    x_shelf[1] = x_shelf[0];
                    x_shelf[0] = output[i];

                    y_shelf[2] = y_shelf[1];
                    y_shelf[1] = y_shelf[0];

                    y_shelf[0] = (float)(b0_shelf * x_shelf[0] + b1_shelf * x_shelf[1] + b2_shelf * x_shelf[2]
                                       - a1_shelf * y_shelf[1] - a2_shelf * y_shelf[2]);
                    enhanced[i] = y_shelf[0];
                }

                // Convert back to PCM bytes
                return ConvertToBytes(enhanced, 1.0f);
            }
            finally
            {
                SetNormalCursor();
            }
        }

        // Helper method to convert float samples back to bytes
        private byte[] ConvertToBytes(float[] samples, float amplification = 1.0f)
        {
            SetBusyCursor();
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