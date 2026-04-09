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

        public byte[] ApplyOverdriveEffect(byte[] input, float drive = 2.2f)
        {
            try
            {
                SetBusyCursor();
                float[] samples = new float[input.Length];
                for (int i = 0; i < input.Length; i++)
                {
                    float sample = (input[i] - 128) / 128.0f;
                    samples[i] = (float)Math.Tanh(sample * drive);
                }

                return ConvertToBytes(samples, 1.0f);
            }
            finally
            {
                SetNormalCursor();
            }
        }

        public byte[] ApplyChorusEffect(byte[] input, int sampleRate)
        {
            try
            {
                SetBusyCursor();
                float[] dry = new float[input.Length];
                for (int i = 0; i < input.Length; i++)
                {
                    dry[i] = (input[i] - 128) / 128.0f;
                }

                float[] wet = new float[input.Length];
                float lfoRateHz = 0.9f;
                float minDelayMs = 7.0f;
                float depthMs = 10.0f;

                for (int i = 0; i < dry.Length; i++)
                {
                    float t = (float)i / sampleRate;
                    float lfo = (float)((Math.Sin(2 * Math.PI * lfoRateHz * t) + 1.0) * 0.5);
                    float delayMs = minDelayMs + (depthMs * lfo);
                    int delaySamples = Math.Max(1, (int)(sampleRate * delayMs / 1000.0f));

                    float delayed = 0;
                    int delayedIndex = i - delaySamples;
                    if (delayedIndex >= 0)
                    {
                        delayed = dry[delayedIndex];
                    }

                    wet[i] = (dry[i] * 0.7f) + (delayed * 0.45f);
                }

                return ConvertToBytes(wet, 1.0f);
            }
            finally
            {
                SetNormalCursor();
            }
        }

        public byte[] ApplyReverseEffect(byte[] input)
        {
            try
            {
                SetBusyCursor();
                byte[] output = new byte[input.Length];
                for (int i = 0; i < input.Length; i++)
                {
                    output[i] = input[input.Length - 1 - i];
                }
                return output;
            }
            finally
            {
                SetNormalCursor();
            }
        }

        public byte[] ApplyFadeIn(byte[] input)
        {
            try
            {
                SetBusyCursor();
                float[] output = new float[input.Length];
                int len = Math.Max(1, input.Length - 1);
                for (int i = 0; i < input.Length; i++)
                {
                    float gain = (float)i / len;
                    float sample = (input[i] - 128) / 128.0f;
                    output[i] = sample * gain;
                }
                return ConvertToBytes(output, 1.0f);
            }
            finally
            {
                SetNormalCursor();
            }
        }

        public byte[] ApplyFadeOut(byte[] input)
        {
            try
            {
                SetBusyCursor();
                float[] output = new float[input.Length];
                int len = Math.Max(1, input.Length - 1);
                for (int i = 0; i < input.Length; i++)
                {
                    float gain = 1.0f - ((float)i / len);
                    float sample = (input[i] - 128) / 128.0f;
                    output[i] = sample * gain;
                }
                return ConvertToBytes(output, 1.0f);
            }
            finally
            {
                SetNormalCursor();
            }
        }

        public byte[] ApplyBandPassEffect(byte[] input, int sampleRate, double centerFrequency, double qFactor)
        {
            try
            {
                SetBusyCursor();
                double w0 = 2.0 * Math.PI * centerFrequency / sampleRate;
                double alpha = Math.Sin(w0) / (2.0 * Math.Max(0.1, qFactor));
                double cosW0 = Math.Cos(w0);

                // Biquad band-pass (constant skirt gain)
                double b0 = alpha;
                double b1 = 0.0;
                double b2 = -alpha;
                double a0 = 1.0 + alpha;
                double a1 = -2.0 * cosW0;
                double a2 = 1.0 - alpha;

                b0 /= a0; b1 /= a0; b2 /= a0;
                a1 /= a0; a2 /= a0;

                float[] output = new float[input.Length];
                float x1 = 0, x2 = 0, y1 = 0, y2 = 0;

                for (int i = 0; i < input.Length; i++)
                {
                    float x0 = (input[i] - 128) / 128.0f;
                    float y0 = (float)(b0 * x0 + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2);
                    output[i] = y0;
                    x2 = x1; x1 = x0;
                    y2 = y1; y1 = y0;
                }

                return ConvertToBytes(output, 1.5f);
            }
            finally
            {
                SetNormalCursor();
            }
        }

        public byte[] ApplyNoiseGate(byte[] input, float threshold = 0.05f, float release = 0.995f)
        {
            try
            {
                SetBusyCursor();
                float[] output = new float[input.Length];
                float gain = 1.0f;

                for (int i = 0; i < input.Length; i++)
                {
                    float sample = (input[i] - 128) / 128.0f;
                    float abs = Math.Abs(sample);

                    if (abs >= threshold)
                    {
                        gain = 1.0f;
                    }
                    else
                    {
                        gain *= release;
                    }

                    output[i] = sample * gain;
                }

                return ConvertToBytes(output, 1.0f);
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
