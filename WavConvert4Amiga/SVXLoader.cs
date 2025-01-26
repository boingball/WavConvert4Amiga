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
        public class SVXInfo
        {
            public byte[] AudioData { get; set; }
            public int SampleRate { get; set; }
            public int LoopStart { get; set; }
            public int LoopEnd { get; set; }
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

                    uint fileLength = (uint)ReverseBytes(reader.ReadInt32());
                    if (fileLength + 8 > fileSize)
                        throw new InvalidDataException("Invalid IFF file size");

                    string fileType = new string(reader.ReadChars(4));
                    if (fileType != "8SVX")
                        throw new InvalidDataException("Not an 8SVX file");

                    SVXInfo info = new SVXInfo
                    {
                        LoopStart = -1,
                        LoopEnd = -1,
                        SampleRate = 8363 // Default if not specified
                    };

                    while (reader.BaseStream.Position < reader.BaseStream.Length - 8)
                    {
                        string chunkName = new string(reader.ReadChars(4));
                        uint chunkSize = (uint)ReverseBytes(reader.ReadInt32());

                        if (reader.BaseStream.Position + chunkSize > fileSize)
                            throw new InvalidDataException("Invalid chunk size");

                        switch (chunkName)
                        {
                            case "VHDR":
                                ProcessVHDRChunk(reader, info, (int)chunkSize);
                                break;

                            case "BODY":
                                info.AudioData = ProcessBODYChunk(reader, (int)chunkSize);
                                break;

                            default:
                                reader.BaseStream.Seek(chunkSize + (chunkSize % 2), SeekOrigin.Current);
                                break;
                        }
                    }

                    if (info.AudioData == null || info.AudioData.Length == 0)
                        throw new InvalidDataException("No audio data found in 8SVX file");

                    return info;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Error loading 8SVX file: {ex.Message}", ex);
            }
        }

        private static void ProcessVHDRChunk(BinaryReader reader, SVXInfo info, int chunkSize)
        {
            if (chunkSize < 14)
                throw new InvalidDataException("VHDR chunk is too small");

            reader.BaseStream.Seek(8, SeekOrigin.Current); // Skip oneShotHiSamples and repeatHiSamples
            reader.BaseStream.Seek(4, SeekOrigin.Current); // Skip samplesPerHiCycle
            // Read sample rate directly as a word value
            ushort sampleRate = (ushort)(reader.ReadByte() << 8 | reader.ReadByte());
            info.SampleRate = sampleRate > 0 ? sampleRate : 8363;
            reader.BaseStream.Seek(chunkSize - 14, SeekOrigin.Current);
        }

        private static byte[] ProcessBODYChunk(BinaryReader reader, int chunkSize)
        {
            if (chunkSize <= 0)
                throw new InvalidDataException("Invalid BODY chunk size");

            byte[] signedData = reader.ReadBytes(chunkSize);
            if (signedData.Length != chunkSize)
                throw new InvalidDataException("Unexpected end of file in BODY chunk");

            byte[] unsignedData = new byte[signedData.Length];
            for (int i = 0; i < signedData.Length; i++)
            {
                unsignedData[i] = (byte)(signedData[i] + 128);
            }
            return unsignedData;
        }

        private static int ReverseBytes(int value)
        {
            uint unsigned = (uint)value;
            return (int)(((unsigned & 0x000000FFu) << 24) |
                        ((unsigned & 0x0000FF00u) << 8) |
                        ((unsigned & 0x00FF0000u) >> 8) |
                        ((unsigned & 0xFF000000u) >> 24));
        }
    }
}