using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WavConvert4Amiga
{
    //STSampleLoader class is used to load ST samples (RAW IFF fomat) into the program
    public class STSampleLoader
    {
        public static bool IsSTSample(string filePath)
        {
            try
            {
                using (var reader = new BinaryReader(File.OpenRead(filePath)))
                {
                    // Check first few bytes against typical ST sample patterns
                    byte[] header = reader.ReadBytes(4);
                    // Pattern 1: Original ST format (F8, FB, FD...)
                    bool isOriginalST = (header[0] >= 0xF0 && header[0] <= 0xFF) &&
                                      (header[1] >= 0xF0 && header[1] <= 0xFF);

                    // Pattern 2: AnalogString format (00, FF, FF...)
                    bool isAnalogFormat = header[0] == 0x00 &&
                                        header[1] == 0xFF &&
                                        header[2] == 0xFF;

                    // Pattern 3: Single-digit ST format (06, 06, 05...)
                    bool isSingleDigitFormat = (header[0] >= 0x00 && header[0] <= 0x0F) &&
                                             (header[1] >= 0x00 && header[1] <= 0x0F) &&
                                             (header[2] >= 0x00 && header[2] <= 0x0F);

                    // Pattern 4: 0x7F repeating format
                    bool is7FFormat = (header[0] == 0x7F) &&
                                    (header[1] == 0x7F) &&
                                    (header[2] == 0x7F);

                    // Pattern 5: 
                    bool is5edFormat = (header[0] == 0x05) &&
                           (header[1] == 0x00) &&
                           (header[2] == 0xED);

                    // Anything Else: Raw ST files seem to have on headers mainly so might just need this
                    bool isOtherFormat = (header[0] >= 0x00);

                    return isOriginalST || isAnalogFormat || isSingleDigitFormat || is7FFormat || is5edFormat || isOtherFormat;
                }
            }
            catch
            {
                return false;
            }
        }

        public static SVXLoader.SVXInfo LoadSTSample(string filePath)
        {
            using (var reader = new BinaryReader(File.OpenRead(filePath)))
            {
                byte[] rawData = reader.ReadBytes((int)reader.BaseStream.Length);

                // Convert signed bytes to unsigned (ST samples are signed)
                for (int i = 0; i < rawData.Length; i++)
                {
                    rawData[i] = (byte)(rawData[i] + 128);
                }

                return new SVXLoader.SVXInfo
                {
                    AudioData = rawData,
                    SampleRate = 8363, // Default ST-xx sample rate
                    LoopStart = -1,
                    LoopEnd = -1
                };
            }
        }
    }
}
