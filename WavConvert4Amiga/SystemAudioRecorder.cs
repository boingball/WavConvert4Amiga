using System;
using System.IO;
using NAudio.Wave;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using System.Diagnostics;

namespace WavConvert4Amiga
{
    public class SystemAudioRecorder
    {
        private NAudio.Wave.WasapiLoopbackCapture wasapiCapture;
        private WaveInEvent waveIn;
        private MemoryStream memoryStream;
        private WaveFileWriter writer;
        private int targetSampleRate;
        private TaskCompletionSource<bool> recordingComplete;
        private readonly object lockObject = new object();

        public byte[] RecordedData { get; private set; }
        public byte[] ProcessedData { get; private set; } // This will store converted data
        public bool IsRecording { get; private set; }
        public WaveFormat CapturedFormat { get; private set; }

        public void StartRecordingSystemSound(int requestedSampleRate)
        {
            if (IsRecording) return;

            try
            {
                recordingComplete = new TaskCompletionSource<bool>();
                targetSampleRate = requestedSampleRate;
                memoryStream = new MemoryStream();

                wasapiCapture = new WasapiLoopbackCapture();
                CapturedFormat = wasapiCapture.WaveFormat;

                // Create a temporary file for writing
                string tempFile = Path.Combine(Path.GetTempPath(), $"temp_recording_{Guid.NewGuid()}.wav");
                writer = new WaveFileWriter(tempFile, wasapiCapture.WaveFormat);

                wasapiCapture.DataAvailable += (s, e) =>
                {
                    if (writer != null && IsRecording)
                    {
                        lock (lockObject)
                        {
                            writer.Write(e.Buffer, 0, e.BytesRecorded);
                        }
                    }
                };

                wasapiCapture.RecordingStopped += (s, e) =>
                {
                    Debug.WriteLine("Recording stopped event received");
                    try
                    {
                        lock (lockObject)
                        {
                            if (writer != null)
                            {
                                writer.Flush();
                                writer.Dispose();
                                writer = null;
                            }
                        }
                        ProcessRecordingFile(tempFile);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in RecordingStopped: {ex.Message}");
                    }
                    finally
                    {
                        try
                        {
                            if (File.Exists(tempFile))
                            {
                                File.Delete(tempFile);
                            }
                        }
                        catch { }
                        recordingComplete.TrySetResult(true);
                    }
                };

                wasapiCapture.StartRecording();
                IsRecording = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting recording: {ex.Message}");
                CleanupResources();
                throw;
            }
        }

        public void StartRecordingMicrophone(int requestedSampleRate)
        {
            if (IsRecording) return;

            try
            {
                recordingComplete = new TaskCompletionSource<bool>();

                // Always record at high quality
                waveIn = new WaveInEvent();
                waveIn.WaveFormat = new WaveFormat(44100, 16, 1); // High quality format
                CapturedFormat = waveIn.WaveFormat;

                // Create a temporary file for writing
                string tempFile = Path.Combine(Path.GetTempPath(), $"temp_recording_{Guid.NewGuid()}.wav");
                writer = new WaveFileWriter(tempFile, waveIn.WaveFormat);

                waveIn.DataAvailable += (s, e) =>
                {
                    if (writer != null && IsRecording)
                    {
                        lock (lockObject)
                        {
                            writer.Write(e.Buffer, 0, e.BytesRecorded);
                        }
                    }
                };

                waveIn.RecordingStopped += (s, e) =>
                {
                    try
                    {
                        lock (lockObject)
                        {
                            if (writer != null)
                            {
                                writer.Flush();
                                writer.Dispose();
                                writer = null;
                            }
                        }
                        ProcessRecordingFile(tempFile);
                    }
                    finally
                    {
                        try
                        {
                            if (File.Exists(tempFile))
                            {
                                File.Delete(tempFile);
                            }
                        }
                        catch { }
                        recordingComplete.TrySetResult(true);
                    }
                };

                waveIn.StartRecording();
                IsRecording = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting mic recording: {ex.Message}");
                CleanupResources();
                throw;
            }
        }

        private void ProcessRecordingFile(string tempFile)
        {
            try
            {
                if (!File.Exists(tempFile))
                {
                    Debug.WriteLine("Temp file not found");
                    return;
                }

                // First, store the high quality data
                using (var reader = new WaveFileReader(tempFile))
                {
                    using (var ms = new MemoryStream())
                    {
                        reader.CopyTo(ms);
                        RecordedData = ms.ToArray();
                    }
                }

                // The high quality data is now stored in RecordedData
                // Any conversion to lower quality should happen in the main form
                // when processing the RecordedData
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing recording: {ex.Message}");
                RecordedData = null;
            }
        }

        public async Task StopRecording()
        {
            if (!IsRecording) return;

            try
            {
                IsRecording = false;
                Debug.WriteLine("Stopping recording...");

                if (wasapiCapture != null && wasapiCapture.CaptureState == CaptureState.Capturing)
                {
                    wasapiCapture.StopRecording();
                }
                if (waveIn != null)
                {
                    waveIn.StopRecording();
                }

                if (recordingComplete != null)
                {
                    await recordingComplete.Task;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping recording: {ex.Message}");
            }
            finally
            {
                CleanupResources();
            }
        }

        private void CleanupResources()
        {
            lock (lockObject)
            {
                if (writer != null)
                {
                    try
                    {
                        writer.Dispose();
                    }
                    catch { }
                    writer = null;
                }
            }

            if (wasapiCapture != null)
            {
                try
                {
                    wasapiCapture.Dispose();
                }
                catch { }
                wasapiCapture = null;
            }

            if (waveIn != null)
            {
                try
                {
                    waveIn.Dispose();
                }
                catch { }
                waveIn = null;
            }

            if (memoryStream != null)
            {
                try
                {
                    memoryStream.Dispose();
                }
                catch { }
                memoryStream = null;
            }
        }
    }
}