using Alissa.Core.Utils;
using NAudio.Wave;
using System;
using System.IO;
using System.Threading;

namespace Alissa.Main
{
    /// <summary>
    /// Plays WAV audio files on the default speaker using NAudio.
    /// Handles temporary file creation and playback synchronization.
    /// Swallows errors to avoid crashing the chat loop.
    /// </summary>
    internal class ConsoleAudioPlayer
    {
        private string _tempDirectory;

        public ConsoleAudioPlayer(string tempDirectory)
        {
            _tempDirectory = tempDirectory;

            // Ensure temp directory exists
            bool dirExists = Directory.Exists(_tempDirectory);
            if (!dirExists)
            {
                Directory.CreateDirectory(_tempDirectory);
            }
        }

        /// <summary>
        /// Play WAV audio bytes on the default speaker.
        /// Blocks until playback completes or an error occurs.
        /// Swallows exceptions and logs to TextManager.Status.
        /// </summary>
        public void PlayWav(byte[] wavBytes)
        {
            bool wavBytesNull = wavBytes == null;
            if (wavBytesNull)
            {
                return;
            }

            bool wavBytesEmpty = wavBytes.Length == 0;
            if (wavBytesEmpty)
            {
                return;
            }

            try
            {
                string tempFileName = Path.Combine(_tempDirectory, $"alissa_audio_{Guid.NewGuid()}.wav");

                // Write WAV bytes to temp file
                File.WriteAllBytes(tempFileName, wavBytes);

                // Play the file
                PlayWavFile(tempFileName);

                // Clean up temp file
                try
                {
                    File.Delete(tempFileName);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            catch (Exception ex)
            {
                TextManager.Status($"[Audio] Error playing WAV: {ex.Message}");
            }
        }

        /// <summary>
        /// Internal method to play a WAV file from disk.
        /// Waits synchronously for playback to complete.
        /// </summary>
        private void PlayWavFile(string filePath)
        {
            WaveOutEvent? waveOutEvent = null;
            WaveFileReader? waveFileReader = null;

            try
            {
                waveOutEvent = new WaveOutEvent();
                waveFileReader = new WaveFileReader(filePath);

                waveOutEvent.Init(waveFileReader);
                waveOutEvent.Play();

                // Wait for playback to complete
                while (waveOutEvent.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(100);
                }
            }
            finally
            {
                // Clean up NAudio resources
                if (waveOutEvent != null)
                {
                    waveOutEvent.Dispose();
                }

                if (waveFileReader != null)
                {
                    waveFileReader.Dispose();
                }
            }
        }
    }
}
