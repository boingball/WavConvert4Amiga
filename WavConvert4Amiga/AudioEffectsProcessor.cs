using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WavConvert4Amiga
{
    public class AudioEffectsProcessor
    {
        private enum ChipWaveType
        {
            Pulse,
            Triangle,
            Saw
        }

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

        public byte[] ApplyChipifyMonoEffect(byte[] input, int sampleRate)
        {
            try
            {
                SetBusyCursor();
                if (input == null || input.Length < 8)
                {
                    return input ?? Array.Empty<byte>();
                }

                float[] source = BytesToFloats(input);
                float[] output = new float[source.Length];

                int frameSize = Math.Max(128, sampleRate / 90); // ~11ms
                int hopSize = Math.Max(64, frameSize / 2);
                int frameCount = Math.Max(1, ((source.Length - 1) / hopSize) + 1);

                float[] pitchTrack = new float[frameCount];
                float[] rmsTrack = new float[frameCount];
                float[] zcrTrack = new float[frameCount];

                for (int frame = 0; frame < frameCount; frame++)
                {
                    int start = frame * hopSize;
                    int end = Math.Min(source.Length, start + frameSize);
                    int len = end - start;
                    if (len < 32)
                    {
                        pitchTrack[frame] = frame > 0 ? pitchTrack[frame - 1] : 220f;
                        rmsTrack[frame] = frame > 0 ? rmsTrack[frame - 1] : 0f;
                        zcrTrack[frame] = frame > 0 ? zcrTrack[frame - 1] : 0.15f;
                        continue;
                    }

                    float freq = EstimateFundamentalFrequency(source, sampleRate, start, len, 85, 1600);
                    pitchTrack[frame] = freq > 0 ? QuantizeFrequencyToSemitone(freq) : (frame > 0 ? pitchTrack[frame - 1] : 220f);
                    rmsTrack[frame] = EstimateRms(source, start, len);
                    zcrTrack[frame] = EstimateZeroCrossingRate(source, start, len);
                }

                float phase = 0f;
                float smoothedFreq = 220f;
                float smoothedEnv = 0f;
                float lowpass = 0f;

                for (int i = 0; i < source.Length; i++)
                {
                    int frame = Math.Min(frameCount - 1, i / hopSize);
                    int nextFrame = Math.Min(frameCount - 1, frame + 1);
                    float frameT = hopSize > 0 ? (float)(i - (frame * hopSize)) / hopSize : 0f;

                    float targetFreq = Lerp(pitchTrack[frame], pitchTrack[nextFrame], frameT);
                    float targetEnv = Lerp(rmsTrack[frame], rmsTrack[nextFrame], frameT);
                    float targetZcr = Lerp(zcrTrack[frame], zcrTrack[nextFrame], frameT);

                    smoothedFreq = (smoothedFreq * 0.90f) + (targetFreq * 0.10f);
                    smoothedEnv = (smoothedEnv * 0.92f) + (targetEnv * 0.08f);

                    float pulseWidth = 0.28f + (Math.Max(0f, Math.Min(1f, targetZcr / 0.35f)) * 0.30f);
                    float increment = smoothedFreq / Math.Max(1, sampleRate);
                    phase += increment;
                    if (phase >= 1f) phase -= 1f;

                    float pulse = phase < pulseWidth ? 1f : -1f;
                    float triangle = 1f - (4f * Math.Abs(phase - 0.5f));
                    float synth = (pulse * 0.80f) + (triangle * 0.20f);

                    // keep attack detail so output still resembles source
                    lowpass = (lowpass * 0.94f) + (source[i] * 0.06f);
                    float transient = source[i] - lowpass;

                    output[i] = (synth * smoothedEnv * 1.25f) + (transient * 0.35f);
                }

                return ConvertToBytes(output, 1.0f);
            }
            finally
            {
                SetNormalCursor();
            }
        }

        public byte[] ApplyChipifyDeluxeEffect(byte[] input, int sampleRate)
        {
            try
            {
                SetBusyCursor();
                if (input == null || input.Length < 8)
                {
                    return input ?? Array.Empty<byte>();
                }

                float[] source = BytesToFloats(input);
                float[] output = new float[source.Length];

                int frameSize = Math.Max(256, sampleRate / 70); // ~14ms
                int hopSize = Math.Max(96, frameSize / 3);
                int frameCount = Math.Max(1, ((source.Length - 1) / hopSize) + 1);

                float[] pitchTrack = new float[frameCount];
                float[] rmsTrack = new float[frameCount];
                float[] zcrTrack = new float[frameCount];
                float[] voicedTrack = new float[frameCount];
                ChipWaveType[] waveTrack = new ChipWaveType[frameCount];

                for (int frame = 0; frame < frameCount; frame++)
                {
                    int start = frame * hopSize;
                    int end = Math.Min(source.Length, start + frameSize);
                    int len = end - start;
                    if (len < 32)
                    {
                        pitchTrack[frame] = frame > 0 ? pitchTrack[frame - 1] : 220f;
                        rmsTrack[frame] = frame > 0 ? rmsTrack[frame - 1] : 0f;
                        zcrTrack[frame] = frame > 0 ? zcrTrack[frame - 1] : 0.2f;
                        voicedTrack[frame] = frame > 0 ? voicedTrack[frame - 1] : 0f;
                        waveTrack[frame] = frame > 0 ? waveTrack[frame - 1] : ChipWaveType.Pulse;
                        continue;
                    }

                    float freq = EstimateFundamentalFrequency(source, sampleRate, start, len, 70, 1800);
                    float zcr = EstimateZeroCrossingRate(source, start, len);
                    float rms = EstimateRms(source, start, len);
                    bool voiced = freq > 0 && zcr < 0.50f && rms > 0.01f;

                    if (voiced)
                    {
                        freq = QuantizeFrequencyToSemitone(freq);
                    }
                    else
                    {
                        freq = frame > 0 ? pitchTrack[frame - 1] : 220f;
                    }

                    pitchTrack[frame] = freq;
                    zcrTrack[frame] = zcr;
                    rmsTrack[frame] = rms;
                    voicedTrack[frame] = voiced ? 1f : 0f;
                    waveTrack[frame] = SelectChipWaveType(zcr, voiced);
                }

                float phase = 0f;
                float smoothedFreq = 220f;
                float smoothedEnv = 0f;

                for (int i = 0; i < source.Length; i++)
                {
                    int frame = Math.Min(frameCount - 1, i / hopSize);
                    int nextFrame = Math.Min(frameCount - 1, frame + 1);
                    float frameT = hopSize > 0 ? (float)(i - (frame * hopSize)) / hopSize : 0f;

                    float targetFreq = Lerp(pitchTrack[frame], pitchTrack[nextFrame], frameT);
                    float targetRms = Lerp(rmsTrack[frame], rmsTrack[nextFrame], frameT);
                    float targetVoiced = Lerp(voicedTrack[frame], voicedTrack[nextFrame], frameT);
                    float targetZcr = Lerp(zcrTrack[frame], zcrTrack[nextFrame], frameT);
                    ChipWaveType wave = frameT < 0.5f ? waveTrack[frame] : waveTrack[nextFrame];

                    smoothedFreq = (smoothedFreq * 0.84f) + (targetFreq * 0.16f);
                    smoothedEnv = (smoothedEnv * 0.88f) + (targetRms * 0.12f);

                    phase += smoothedFreq / Math.Max(1, sampleRate);
                    if (phase >= 1f) phase -= 1f;

                    float baseWave = GenerateChipSample(wave, phase);
                    float harmonic2 = GenerateChipSample(ChipWaveType.Saw, (phase * 2f) % 1f) * 0.25f;
                    float harmonic3 = GenerateChipSample(ChipWaveType.Pulse, (phase * 3f) % 1f) * 0.12f;
                    float synth = (baseWave + harmonic2 + harmonic3) * smoothedEnv * 1.2f;

                    float noise = ((((i * 1103515245) + 12345) & 0x7fff) / 16384.0f - 1.0f) * 0.25f * (1f - targetVoiced);
                    float dryBlend = source[i] * (0.18f + (Math.Max(0f, Math.Min(1f, targetZcr)) * 0.10f));

                    output[i] = synth + noise + dryBlend;
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

        private float[] BytesToFloats(byte[] input)
        {
            float[] output = new float[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = (input[i] - 128) / 128.0f;
            }
            return output;
        }

        private float EstimateFundamentalFrequency(float[] source, int sampleRate, int start, int length)
        {
            return EstimateFundamentalFrequency(source, sampleRate, start, length, 70, 1400);
        }

        private float EstimateFundamentalFrequency(float[] source, int sampleRate, int start, int length, int minFrequency, int maxFrequency)
        {
            if (length <= 0 || sampleRate <= 0)
            {
                return 0f;
            }

            int clampedMinFrequency = Math.Max(30, minFrequency);
            int clampedMaxFrequency = Math.Max(clampedMinFrequency + 1, maxFrequency);

            int minLag = Math.Max(1, sampleRate / clampedMaxFrequency);
            int maxLag = Math.Min(length - 2, sampleRate / clampedMinFrequency);
            if (maxLag <= minLag)
            {
                return 0f;
            }

            float best = 0f;
            int bestLag = 0;

            for (int lag = minLag; lag <= maxLag; lag++)
            {
                float corr = 0f;
                float normA = 0f;
                float normB = 0f;
                int end = start + length - lag;

                for (int i = start; i < end; i++)
                {
                    float a = source[i];
                    float b = source[i + lag];
                    corr += a * b;
                    normA += a * a;
                    normB += b * b;
                }

                if (normA <= 1e-7f || normB <= 1e-7f)
                {
                    continue;
                }

                float normalized = (float)(corr / Math.Sqrt(normA * normB));
                if (normalized > best)
                {
                    best = normalized;
                    bestLag = lag;
                }
            }

            if (bestLag == 0 || best < 0.25f)
            {
                return 0f;
            }

            return (float)sampleRate / bestLag;
        }

        private float EstimateRms(float[] source, int start, int length)
        {
            if (length <= 0)
            {
                return 0f;
            }

            float sum = 0f;
            int end = start + length;
            for (int i = start; i < end; i++)
            {
                sum += source[i] * source[i];
            }

            return (float)Math.Sqrt(sum / length);
        }

        private float Lerp(float a, float b, float t)
        {
            return a + ((b - a) * Math.Max(0f, Math.Min(1f, t)));
        }

        private float QuantizeFrequencyToSemitone(float frequency)
        {
            if (frequency <= 0f)
            {
                return 0f;
            }

            float midi = 69f + (12f * (float)(Math.Log(frequency / 440.0f, 2)));
            float quantizedMidi = (float)Math.Round(midi);
            float quantized = 440f * (float)Math.Pow(2, (quantizedMidi - 69f) / 12f);
            return Math.Max(55f, Math.Min(1760f, quantized));
        }

        private float EstimateZeroCrossingRate(float[] source, int start, int length)
        {
            if (length <= 1)
            {
                return 0f;
            }

            int crosses = 0;
            int end = start + length;
            for (int i = start + 1; i < end; i++)
            {
                bool prevNeg = source[i - 1] < 0f;
                bool currNeg = source[i] < 0f;
                if (prevNeg != currNeg)
                {
                    crosses++;
                }
            }

            return (float)crosses / (length - 1);
        }

        private ChipWaveType SelectChipWaveType(float zeroCrossingRate, bool voiced)
        {
            if (!voiced)
            {
                return ChipWaveType.Pulse;
            }

            if (zeroCrossingRate < 0.10f)
            {
                return ChipWaveType.Triangle;
            }

            if (zeroCrossingRate < 0.22f)
            {
                return ChipWaveType.Pulse;
            }

            return ChipWaveType.Saw;
        }

        private float GenerateChipSample(ChipWaveType wave, float phase)
        {
            switch (wave)
            {
                case ChipWaveType.Triangle:
                    return 1f - (4f * Math.Abs(phase - 0.5f));
                case ChipWaveType.Saw:
                    return (2f * phase) - 1f;
                case ChipWaveType.Pulse:
                default:
                    return phase < 0.35f ? 1f : -1f;
            }
        }
    }
}
