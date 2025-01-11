using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WavConvert4Amiga
{
    public class RecordedAudioData
    {
        public byte[] RawData { get; private set; }
        public WaveFormat OriginalFormat { get; private set; }

        public RecordedAudioData(byte[] data, WaveFormat format)
        {
            RawData = data;
            OriginalFormat = format;
        }

        public byte[] GetResampledData(int targetSampleRate)
        {
            if (RawData == null || RawData.Length == 0) return null;

            using (var inputStream = new MemoryStream(RawData))
            using (var reader = new RawSourceWaveStream(inputStream, OriginalFormat))
            {
                var targetFormat = new WaveFormat(targetSampleRate, 8, 1);

                // If formats match, return original data
                if (OriginalFormat.SampleRate == targetSampleRate &&
                    OriginalFormat.BitsPerSample == 8 &&
                    OriginalFormat.Channels == 1)
                {
                    return RawData;
                }

                using (var resampler = new MediaFoundationResampler(reader, targetFormat))
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

}
