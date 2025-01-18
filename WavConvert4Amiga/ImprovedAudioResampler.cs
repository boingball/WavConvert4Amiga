using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WavConvert4Amiga
{
    public class ImprovedAudioResampler
    {
        private WaveFormat originalFormat;
        private int originalSampleRate;
        private byte[] originalData;

        public void SetOriginalData(byte[] data, WaveFormat format)
        {
            originalData = data;
            originalFormat = format;
            originalSampleRate = format.SampleRate;
        }

        public byte[] ResampleToRate(int targetSampleRate, out float timeScaleFactor)
        {
            if (originalData == null || originalFormat == null)
                throw new InvalidOperationException("Original data not set");

            timeScaleFactor = (float)targetSampleRate / originalSampleRate;

            // Create source stream
            using (var sourceMs = new MemoryStream())
            {
                using (var writer = new WaveFileWriter(sourceMs, originalFormat))
                {
                    writer.Write(originalData, 0, originalData.Length);
                    writer.Flush();
                    sourceMs.Position = 0;

                    // Create target format maintaining original channels and bits
                    var targetFormat = new WaveFormat(
                        targetSampleRate,
                        originalFormat.BitsPerSample,
                        originalFormat.Channels);

                    using (var reader = new WaveFileReader(sourceMs))
                    using (var resampler = new MediaFoundationResampler(reader, targetFormat))
                    {
                        resampler.ResamplerQuality = 60; // High quality

                        // Calculate expected output length
                        long expectedOutputLength = (long)(originalData.Length * timeScaleFactor);

                        using (var outStream = new MemoryStream())
                        {
                            byte[] buffer = new byte[4096];
                            int read;
                            long totalBytesRead = 0;

                            while ((read = resampler.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                outStream.Write(buffer, 0, read);
                                totalBytesRead += read;
                            }

                            // Convert to 8-bit mono if needed
                            var resampledData = outStream.ToArray();
                            if (targetFormat.BitsPerSample != 8 || targetFormat.Channels != 1)
                            {
                                return ConvertTo8BitMono(resampledData, targetFormat);
                            }

                            return resampledData;
                        }
                    }
                }
            }
        }

        private byte[] ConvertTo8BitMono(byte[] data, WaveFormat format)
        {
            // Number of bytes per sample for all channels
            int bytesPerSample = (format.BitsPerSample / 8) * format.Channels;
            int samples = data.Length / bytesPerSample;
            byte[] converted = new byte[samples];

            for (int i = 0; i < samples; i++)
            {
                int sampleOffset = i * bytesPerSample;
                float sum = 0;

                // Average all channels
                for (int channel = 0; channel < format.Channels; channel++)
                {
                    int channelOffset = sampleOffset + (channel * (format.BitsPerSample / 8));
                    float sample = 0;

                    // Convert based on bit depth
                    switch (format.BitsPerSample)
                    {
                        case 16:
                            short val16 = BitConverter.ToInt16(data, channelOffset);
                            sample = val16 / 32768f;
                            break;
                        case 24:
                            int val24 = (data[channelOffset + 2] << 16) |
                                      (data[channelOffset + 1] << 8) |
                                       data[channelOffset];
                            if ((val24 & 0x800000) != 0)
                                val24 |= ~0xFFFFFF; // Sign extend
                            sample = val24 / 8388608f;
                            break;
                        case 32:
                            int val32 = BitConverter.ToInt32(data, channelOffset);
                            sample = val32 / 2147483648f;
                            break;
                    }
                    sum += sample;
                }

                // Average channels and convert to 8-bit unsigned
                float average = sum / format.Channels;
                converted[i] = (byte)(((average + 1f) / 2f) * 255f);
            }

            return converted;
        }
    }
}
