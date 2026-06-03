using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

 namespace WavConvert4Amiga
    {
    public class SVXLoader
    {
        private const byte sCmpNone = 0;
        public const byte sCmpFibDelta = 1;
        private static readonly int[] CodeToDelta = { -34, -21, -13, -8, -5, -3, -2, -1, 0, 1, 2, 3, 5, 8, 13, 21 };

        public class SVXInfo
        {
            public byte[] AudioData { get; set; }
            public int SampleRate { get; set; }
            public int LoopStart { get; set; }
            public int LoopEnd { get; set; }
            public byte Compression { get; set; }
            public int TotalSamples { get; set; }
            public bool IsFibonacciDeltaCompressed => Compression == sCmpFibDelta;
        }

        public static SVXInfo Load8SVXFile(string filePath)
        {
            try
            {
                using (var reader = new BinaryReader(File.OpenRead(filePath)))
                {
                    long fileSize = reader.BaseStream.Length;
                    if (fileSize < 12) // Minimum size for FORM + size + 8SVX
                        throw new InvalidDataException("File is too small to be a valid 8SVX");

                    // Check FORM header
                    string formType = new string(reader.ReadChars(4));
                    if (formType != "FORM")
                        throw new InvalidDataException("Not a valid IFF file (missing FORM header)");

                    uint fileLength = ReadUInt32BigEndian(reader);
                    if (fileLength + 8 > fileSize)
                        throw new InvalidDataException("Invalid IFF file size");

                    string fileType = new string(reader.ReadChars(4));
                    if (fileType != "8SVX")
                        throw new InvalidDataException("Not an 8SVX file");

                    SVXInfo info = new SVXInfo
                    {
                        LoopStart = -1,
                        LoopEnd = -1,
                        SampleRate = 8363, // Default if not specified
                        Compression = sCmpNone
                    };

                    byte[] bodyData = null;
                    while (reader.BaseStream.Position < reader.BaseStream.Length - 8)
                    {
                        string chunkName = new string(reader.ReadChars(4));
                        uint chunkSize = ReadUInt32BigEndian(reader);

                        if (reader.BaseStream.Position + chunkSize > fileSize)
                            throw new InvalidDataException("Invalid chunk size");

                        long chunkDataStart = reader.BaseStream.Position;
                        switch (chunkName)
                        {
                            case "VHDR":
                                ProcessVHDRChunk(reader, info, (int)chunkSize);
                                break;

                            case "BODY":
                                bodyData = ReadBODYChunk(reader, (int)chunkSize);
                                break;

                            default:
                                reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                                break;
                        }

                        long chunkDataEnd = chunkDataStart + chunkSize;
                        if (reader.BaseStream.Position < chunkDataEnd)
                        {
                            reader.BaseStream.Seek(chunkDataEnd, SeekOrigin.Begin);
                        }

                        if ((chunkSize & 1) == 1 && reader.BaseStream.Position < reader.BaseStream.Length)
                        {
                            reader.BaseStream.Seek(1, SeekOrigin.Current);
                        }
                    }

                    if (bodyData == null || bodyData.Length == 0)
                        throw new InvalidDataException("No audio data found in 8SVX file");

                    byte[] signedData;
                    if (info.Compression == sCmpFibDelta)
                    {
                        signedData = DecompressFibonacciDeltaSigned(bodyData);
                    }
                    else if (info.Compression == sCmpNone)
                    {
                        signedData = bodyData;
                    }
                    else
                    {
                        throw new InvalidDataException($"Unsupported 8SVX compression type: {info.Compression}");
                    }

                    int declaredSampleCount = GetDeclaredSampleCount(info);
                    if (info.Compression == sCmpFibDelta &&
                        declaredSampleCount > 0 &&
                        signedData.Length == declaredSampleCount + 1)
                    {
                        // Odd sample counts are stored as a final padded nybble, so drop only that synthetic sample.
                        Array.Resize(ref signedData, declaredSampleCount);
                    }

                    info.AudioData = ConvertSignedToUnsigned(signedData);
                    return info;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Error loading 8SVX file: {ex.Message}", ex);
            }
        }

        public static byte[] CompressFibonacciDelta(byte[] unsignedPcmData)
        {
            if (unsignedPcmData == null)
                throw new ArgumentNullException(nameof(unsignedPcmData));

            byte[] signedData = new byte[unsignedPcmData.Length];
            for (int i = 0; i < unsignedPcmData.Length; i++)
            {
                signedData[i] = unchecked((byte)(unsignedPcmData[i] - 128));
            }

            return CompressFibonacciDeltaSigned(signedData);
        }

        public static byte[] RoundTripFibonacciDelta(byte[] unsignedPcmData)
        {
            if (unsignedPcmData == null)
                return null;

            byte[] compressed = CompressFibonacciDelta(unsignedPcmData);
            byte[] decompressedSigned = DecompressFibonacciDeltaSigned(compressed);
            if (decompressedSigned.Length > unsignedPcmData.Length)
            {
                Array.Resize(ref decompressedSigned, unsignedPcmData.Length);
            }

            return ConvertSignedToUnsigned(decompressedSigned);
        }

        private static void ProcessVHDRChunk(BinaryReader reader, SVXInfo info, int chunkSize)
        {
            if (chunkSize < 20)
                throw new InvalidDataException("VHDR chunk is too small");

            uint oneShotHiSamples = ReadUInt32BigEndian(reader);
            uint repeatHiSamples = ReadUInt32BigEndian(reader);
            ReadUInt32BigEndian(reader); // samplesPerHiCycle
            ushort sampleRate = ReadUInt16BigEndian(reader);
            info.SampleRate = sampleRate > 0 ? sampleRate : 8363;
            reader.ReadByte(); // ctOctave
            info.Compression = reader.ReadByte();
            ulong totalSamples = oneShotHiSamples + repeatHiSamples;
            info.TotalSamples = (int)Math.Min(totalSamples, (ulong)int.MaxValue);
            reader.BaseStream.Seek(4, SeekOrigin.Current); // volume

            if (repeatHiSamples > 0)
            {
                info.LoopStart = (int)Math.Min(oneShotHiSamples, int.MaxValue);
                ulong loopEnd = oneShotHiSamples + repeatHiSamples;
                info.LoopEnd = (int)Math.Min(loopEnd, (ulong)int.MaxValue);
            }
        }

        private static byte[] ReadBODYChunk(BinaryReader reader, int chunkSize)
        {
            if (chunkSize <= 0)
                throw new InvalidDataException("Invalid BODY chunk size");

            byte[] data = reader.ReadBytes(chunkSize);
            if (data.Length != chunkSize)
                throw new InvalidDataException("Unexpected end of file in BODY chunk");

            return data;
        }

        private static byte[] CompressFibonacciDeltaSigned(byte[] signedData)
        {
            byte initialValue = signedData.Length > 0 ? signedData[0] : (byte)0;
            int encodedByteCount = (signedData.Length + 1) / 2;
            byte[] body = new byte[encodedByteCount + 2];
            body[0] = 0; // Pad byte required by the 8SVX Fibonacci-delta BODY format.
            body[1] = initialValue;

            byte predictor = initialValue;
            for (int i = 0; i < signedData.Length; i++)
            {
                int code = FindClosestDeltaCode(unchecked((sbyte)signedData[i]) - unchecked((sbyte)predictor));
                predictor = unchecked((byte)(predictor + CodeToDelta[code]));

                int outputIndex = 2 + (i / 2);
                if ((i & 1) == 0)
                {
                    body[outputIndex] = (byte)(code << 4);
                }
                else
                {
                    body[outputIndex] |= (byte)code;
                }
            }

            return body;
        }

        private static byte[] DecompressFibonacciDeltaSigned(byte[] bodyData)
        {
            if (bodyData.Length < 2)
                throw new InvalidDataException("Compressed 8SVX BODY chunk is too small");

            byte predictor = bodyData[1];
            byte[] signedData = new byte[(bodyData.Length - 2) * 2];
            int outputIndex = 0;
            for (int i = 2; i < bodyData.Length; i++)
            {
                byte packedCodes = bodyData[i];
                predictor = unchecked((byte)(predictor + CodeToDelta[(packedCodes >> 4) & 0x0F]));
                signedData[outputIndex++] = predictor;
                predictor = unchecked((byte)(predictor + CodeToDelta[packedCodes & 0x0F]));
                signedData[outputIndex++] = predictor;
            }

            return signedData;
        }

        private static int FindClosestDeltaCode(int targetDelta)
        {
            int bestCode = 0;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < CodeToDelta.Length; i++)
            {
                int distance = Math.Abs(targetDelta - CodeToDelta[i]);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCode = i;
                }
            }

            return bestCode;
        }

        private static byte[] ConvertSignedToUnsigned(byte[] signedData)
        {
            byte[] unsignedData = new byte[signedData.Length];
            for (int i = 0; i < signedData.Length; i++)
            {
                unsignedData[i] = unchecked((byte)(signedData[i] + 128));
            }
            return unsignedData;
        }

        private static int GetDeclaredSampleCount(SVXInfo info)
        {
            if (info.TotalSamples > 0)
            {
                return info.TotalSamples;
            }

            int sampleCount = 0;
            if (info.LoopStart > 0)
            {
                sampleCount += info.LoopStart;
            }
            if (info.LoopEnd > info.LoopStart)
            {
                sampleCount += info.LoopEnd - info.LoopStart;
            }
            return sampleCount;
        }

        private static ushort ReadUInt16BigEndian(BinaryReader reader)
        {
            return (ushort)((reader.ReadByte() << 8) | reader.ReadByte());
        }

        private static uint ReadUInt32BigEndian(BinaryReader reader)
        {
            return ((uint)reader.ReadByte() << 24) |
                   ((uint)reader.ReadByte() << 16) |
                   ((uint)reader.ReadByte() << 8) |
                   reader.ReadByte();
        }
    }
}
