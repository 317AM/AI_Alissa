using Alissa.Core.Utils;
using NAudio.Wave;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Alissa.Main
{
    /// <summary>
    /// Records audio from the default microphone for the /talk command.
    /// Records until the user presses Enter (via console ReadLine).
    /// Max 20 seconds. Returns 16 kHz mono 16-bit PCM WAV bytes.
    /// </summary>
    internal class MicrophoneRecorder
    {
        private const int TARGET_SAMPLE_RATE = 16000;
        private const int TARGET_CHANNELS = 1;
        private const int BITS_PER_SAMPLE = 16;
        private const int MAX_RECORDING_SECONDS = 20;
        private const int MIN_RECORDING_MS = 300;

        /// <summary>
        /// Record audio until the user presses Enter or timeout occurs.
        /// Returns 16 kHz mono PCM WAV bytes.
        /// Returns empty array if recording is shorter than 300 ms.
        /// </summary>
        public async Task<byte[]> RecordUntilEnterAsync(CancellationToken ct = default)
        {
            byte[] result = Array.Empty<byte>();

            try
            {
                TextManager.Status("Recording. Speak, then press Enter to stop.");

                WaveInEvent? waveInEvent = null;
                System.Collections.Generic.List<byte> audioBuffer = new System.Collections.Generic.List<byte>();
                DateTime recordingStartTime = DateTime.Now;
                bool recordingTimedOut = false;

                try
                {
                    waveInEvent = new WaveInEvent();
                    waveInEvent.WaveFormat = new WaveFormat(TARGET_SAMPLE_RATE, BITS_PER_SAMPLE, TARGET_CHANNELS);

                    waveInEvent.DataAvailable += (s, e) =>
                    {
                        audioBuffer.AddRange(e.Buffer.AsSpan(0, e.BytesRecorded).ToArray());
                    };

                    waveInEvent.StartRecording();

                    // Record while waiting for user to press Enter
                    Task readLineTask = Task.Run(() => Console.ReadLine());

                    Task recordingTimeoutTask = Task.Delay(MAX_RECORDING_SECONDS * 1000, ct);

                    Task completedTask = await Task.WhenAny(readLineTask, recordingTimeoutTask);

                    bool timeoutOccurred = completedTask == recordingTimeoutTask;
                    if (timeoutOccurred)
                    {
                        recordingTimedOut = true;
                    }

                    waveInEvent.StopRecording();

                    // Wait briefly for final buffers
                    await Task.Delay(100, ct);
                }
                finally
                {
                    if (waveInEvent != null)
                    {
                        waveInEvent.Dispose();
                    }
                }

                // Check if recording is long enough
                TimeSpan recordingDuration = DateTime.Now - recordingStartTime;
                bool recordingTooShort = recordingDuration.TotalMilliseconds < MIN_RECORDING_MS;
                if (recordingTooShort)
                {
                    TextManager.Status("[Mic] Recording too short, discarded.");
                    return Array.Empty<byte>();
                }

                // Convert raw PCM buffer to WAV format
                result = ConvertPcmToWav(audioBuffer.ToArray());
            }
            catch (Exception ex)
            {
                TextManager.Status($"[Mic] Error during recording: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Convert raw PCM bytes to WAV format with proper header.
        /// Assumes 16-bit PCM, 16000 Hz, 1 channel (mono).
        /// </summary>
        private byte[] ConvertPcmToWav(byte[] pcmData)
        {
            int sampleRate = TARGET_SAMPLE_RATE;
            int channels = TARGET_CHANNELS;
            int bytesPerSample = BITS_PER_SAMPLE / 8;
            int byteRate = sampleRate * channels * bytesPerSample;
            int blockAlign = channels * bytesPerSample;

            byte[] wavHeader = new byte[44];

            // RIFF chunk descriptor
            System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(wavHeader, 0);
            int fileSize = 36 + pcmData.Length;
            BitConverter.GetBytes(fileSize).CopyTo(wavHeader, 4);
            System.Text.Encoding.ASCII.GetBytes("WAVE").CopyTo(wavHeader, 8);

            // fmt sub-chunk
            System.Text.Encoding.ASCII.GetBytes("fmt ").CopyTo(wavHeader, 12);
            BitConverter.GetBytes(16).CopyTo(wavHeader, 16); // Subchunk1Size
            BitConverter.GetBytes((short)1).CopyTo(wavHeader, 20); // PCM format
            BitConverter.GetBytes((short)channels).CopyTo(wavHeader, 22);
            BitConverter.GetBytes(sampleRate).CopyTo(wavHeader, 24);
            BitConverter.GetBytes(byteRate).CopyTo(wavHeader, 28);
            BitConverter.GetBytes((short)blockAlign).CopyTo(wavHeader, 32);
            BitConverter.GetBytes((short)BITS_PER_SAMPLE).CopyTo(wavHeader, 34);

            // data sub-chunk
            System.Text.Encoding.ASCII.GetBytes("data").CopyTo(wavHeader, 36);
            BitConverter.GetBytes(pcmData.Length).CopyTo(wavHeader, 40);

            // Combine header and PCM data
            byte[] result = new byte[wavHeader.Length + pcmData.Length];
            Array.Copy(wavHeader, 0, result, 0, wavHeader.Length);
            Array.Copy(pcmData, 0, result, wavHeader.Length, pcmData.Length);

            return result;
        }
    }
}
