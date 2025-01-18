using System;
using System.IO;
using NAudio.Wave;

namespace WavConvert4Amiga
{
    public class SimpleLoopingProvider : ISampleProvider
    {
        private readonly ISampleProvider sourceProvider;
        private readonly int loopStartSample;
        private readonly int loopEndSample;
        private int position = 0;
        private readonly int startPosition;
        private bool enableLooping;

        public SimpleLoopingProvider(ISampleProvider sourceProvider, int loopStartSample, int loopEndSample, bool enableLooping = true)
        {
            this.sourceProvider = sourceProvider;
            this.loopStartSample = loopStartSample;
            this.loopEndSample = loopEndSample;
            this.enableLooping = enableLooping;
            this.startPosition = 0;
        }

        public WaveFormat WaveFormat => sourceProvider.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                int bytesRead = sourceProvider.Read(buffer, offset + totalBytesRead, count - totalBytesRead);

                if (bytesRead == 0)
                {
                    if (enableLooping)
                    {
                        position = loopStartSample;
                        if (sourceProvider is RawSourceWaveStream rawSource)
                        {
                            rawSource.Position = loopStartSample;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                totalBytesRead += bytesRead;
                position += bytesRead;

                if (position >= loopEndSample && enableLooping)
                {
                    position = loopStartSample;
                    if (sourceProvider is RawSourceWaveStream rawSource)
                    {
                        rawSource.Position = loopStartSample;
                    }
                }
            }

            return totalBytesRead;
        }

        public void EnableLooping(bool enable)
        {
            enableLooping = enable;
        }
    }

    public class LoopingWaveProvider : IWaveProvider
    {
        private readonly IWaveProvider sourceProvider;
        private readonly int loopStartByte;
        private readonly int loopEndByte;
        private long position;
        private bool isLooping;
        private WaveFormat waveFormat;

        public LoopingWaveProvider(IWaveProvider sourceProvider, int loopStartByte, int loopEndByte)
        {
            this.sourceProvider = sourceProvider;
            this.loopStartByte = loopStartByte;
            this.loopEndByte = loopEndByte;
            this.position = 0;
            this.isLooping = true;
            this.waveFormat = sourceProvider.WaveFormat;
        }

        public WaveFormat WaveFormat => waveFormat;

        public int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                if (position >= loopEndByte && isLooping)
                {
                    position = loopStartByte;
                    if (sourceProvider is RawSourceWaveStream rawSource)
                    {
                        rawSource.Position = loopStartByte;
                    }
                }

                int bytesToRead = count - totalBytesRead;
                if (isLooping && position + bytesToRead > loopEndByte)
                {
                    bytesToRead = loopEndByte - (int)position;
                }

                int bytesRead = sourceProvider.Read(buffer, offset + totalBytesRead, bytesToRead);
                if (bytesRead == 0)
                {
                    if (isLooping)
                    {
                        position = loopStartByte;
                        if (sourceProvider is RawSourceWaveStream rawSource)
                        {
                            rawSource.Position = loopStartByte;
                        }
                        continue;
                    }
                    break;
                }

                totalBytesRead += bytesRead;
                position += bytesRead;
            }

            return totalBytesRead;
        }

        public void EnableLooping(bool enable)
        {
            isLooping = enable;
        }
    }
}