using NAudio.Wave;
using System.Collections.Generic;
using System.IO;
using System;

public class AudioProcessor
{
    private byte[] originalData;
    private byte[] workingData;
    private WaveFormat originalFormat;
    private int currentSampleRate;
    private float amplificationFactor = 1.0f;
    private List<(int start, int end)> cutRegions = new List<(int start, int end)>();

    // Initialize or reset the processor with new data
    public void SetOriginalData(byte[] data, WaveFormat format)
    {
        if (data == null || format == null)
            throw new ArgumentNullException("Data and format must not be null");

        originalData = new byte[data.Length];
        workingData = new byte[data.Length];
        Buffer.BlockCopy(data, 0, originalData, 0, data.Length);
        Buffer.BlockCopy(data, 0, workingData, 0, data.Length);

        originalFormat = format;
        currentSampleRate = format.SampleRate;
        amplificationFactor = 1.0f;
        cutRegions.Clear();
    }

    // Process the audio data with current settings
    public byte[] ProcessAudio(int targetSampleRate)
    {
        if (originalData == null || originalFormat == null)
            throw new InvalidOperationException("Original data not set");

        try
        {
            // Start from original data for resampling
            byte[] currentData = new byte[originalData.Length];
            Buffer.BlockCopy(originalData, 0, currentData, 0, originalData.Length);

            // Step 1: Resample if needed
            if (targetSampleRate != originalFormat.SampleRate)
            {
                currentData = ResampleAudio(currentData, targetSampleRate);
            }

            // Step 2: Apply cuts if any
            if (cutRegions.Count > 0)
            {
                currentData = ApplyCuts(currentData, targetSampleRate);
            }

            // Step 3: Apply amplification
            if (amplificationFactor != 1.0f)
            {
                currentData = ApplyAmplification(currentData);
            }

            // Update working data
            workingData = new byte[currentData.Length];
            Buffer.BlockCopy(currentData, 0, workingData, 0, currentData.Length);
            currentSampleRate = targetSampleRate;

            return currentData;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error processing audio: {ex.Message}", ex);
        }
    }

    private byte[] ResampleAudio(byte[] data, int targetSampleRate)
    {
        using (var sourceMs = new MemoryStream())
        {
            var sourceFormat = new WaveFormat(currentSampleRate, 8, 1);
            using (var writer = new WaveFileWriter(sourceMs, sourceFormat))
            {
                writer.Write(data, 0, data.Length);
                writer.Flush();
                sourceMs.Position = 0;

                using (var reader = new WaveFileReader(sourceMs))
                using (var resampler = new MediaFoundationResampler(reader, new WaveFormat(targetSampleRate, 8, 1)))
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
                        return outStream.ToArray();
                    }
                }
            }
        }
    }

    private byte[] ApplyCuts(byte[] data, int currentRate)
    {
        if (cutRegions.Count == 0) return data;

        var scaledCuts = ScaleCutRegions(currentRate);
        byte[] result = data;

        foreach (var cut in scaledCuts.OrderByDescending(c => c.start))
        {
            if (cut.start < 0 || cut.end > result.Length || cut.start >= cut.end)
                continue;

            int newLength = result.Length - (cut.end - cut.start);
            if (newLength <= 0) continue;

            byte[] newData = new byte[newLength];

            // Copy before cut
            if (cut.start > 0)
            {
                Buffer.BlockCopy(result, 0, newData, 0, cut.start);
            }

            // Copy after cut
            if (cut.end < result.Length)
            {
                int afterCutLength = result.Length - cut.end;
                Buffer.BlockCopy(result, cut.end, newData, cut.start, afterCutLength);
            }

            result = newData;
        }

        return result;
    }

    private byte[] ApplyAmplification(byte[] data)
    {
        byte[] result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            float sample = (data[i] - 128) * amplificationFactor;
            result[i] = (byte)Math.Max(0, Math.Min(255, sample + 128));
        }
        return result;
    }

    private List<(int start, int end)> ScaleCutRegions(int targetRate)
    {
        float scaleFactor = (float)targetRate / originalFormat.SampleRate;
        return cutRegions.Select(cut => (
            start: (int)(cut.start * scaleFactor),
            end: (int)(cut.end * scaleFactor)
        )).ToList();
    }

    public void AddCut(int start, int end)
    {
        if (start < 0 || end < 0 || start >= end)
            throw new ArgumentException("Invalid cut region");

        // Store cuts in original sample rate coordinates
        float scaleFactor = (float)originalFormat.SampleRate / currentSampleRate;
        int originalStart = (int)(start * scaleFactor);
        int originalEnd = (int)(end * scaleFactor);

        cutRegions.Add((originalStart, originalEnd));
    }

    public void SetAmplification(float factor)
    {
        amplificationFactor = factor;
    }

    public float GetTimeScaleFactor(int targetSampleRate)
    {
        return (float)targetSampleRate / originalFormat.SampleRate;
    }

    public void ClearCuts()
    {
        cutRegions.Clear();
    }
}