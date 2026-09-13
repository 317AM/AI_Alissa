using System;
using System.IO;

namespace Alissa.Core.Utils
{
    /// <summary>
    /// Shifts the pitch of WAV audio by resampling the audio data.
    /// This is a simple approach: multiply the sample rate in the WAV header
    /// by 2^(semitones/12) to achieve pitch shift.
    /// </summary>
    public class WavPitchShifter
    {
        /// <summary>
        /// Shift the pitch of WAV audio data by the given number of semitones.
        /// Returns the modified WAV bytes.
        /// If semitones == 0, returns the input bytes unchanged.
        /// </summary>
        public static byte[] ShiftPitch(byte[] wavBytes, double semitones)
        {
            bool noShift = Math.Abs(semitones) < 0.001;
            if (noShift)
            {
                return wavBytes;
            }

            try
            {
                using (MemoryStream ms = new MemoryStream(wavBytes))
                {
                    // Read WAV header
                    byte[] header = new byte[44];
                    int headerRead = ms.Read(header, 0, 44);
                    bool headerTooShort = headerRead < 44;
                    if (headerTooShort)
                    {
                        return wavBytes; // Not a valid WAV, return unchanged
                    }

                    // Check RIFF and WAVE markers
                    string riffMarker = System.Text.Encoding.ASCII.GetString(header, 0, 4);
                    string waveMarker = System.Text.Encoding.ASCII.GetString(header, 8, 4);
                    bool invalidFormat = riffMarker != "RIFF" || waveMarker != "WAVE";
                    if (invalidFormat)
                    {
                        return wavBytes;
                    }

                    // Extract sample rate from header (bytes 24-27, little-endian)
                    int originalSampleRate = BitConverter.ToInt32(header, 24);

                    // Calculate new sample rate with pitch shift
                    double pitchFactor = Math.Pow(2.0, semitones / 12.0);
                    int newSampleRate = (int)Math.Round(originalSampleRate * pitchFactor);

                    // Update sample rate in header
                    byte[] newSampleRateBytes = BitConverter.GetBytes(newSampleRate);
                    Array.Copy(newSampleRateBytes, 0, header, 24, 4);

                    // Also update byte rate (bytes 28-31)
                    // byte_rate = sample_rate * num_channels * bytes_per_sample
                    int bytesPerSample = BitConverter.ToInt16(header, 34) / 8;
                    int channels = BitConverter.ToInt16(header, 22);
                    int newByteRate = newSampleRate * channels * bytesPerSample;
                    byte[] newByteRateBytes = BitConverter.GetBytes(newByteRate);
                    Array.Copy(newByteRateBytes, 0, header, 28, 4);

                    // Read the audio data
                    ms.Seek(44, SeekOrigin.Begin);
                    byte[] audioData = new byte[ms.Length - 44];
                    ms.Read(audioData, 0, audioData.Length);

                    // Reconstruct the WAV file
                    using (MemoryStream result = new MemoryStream())
                    {
                        result.Write(header, 0, 44);
                        result.Write(audioData, 0, audioData.Length);
                        return result.ToArray();
                    }
                }
            }
            catch
            {
                // On any error, return the original bytes unchanged
                return wavBytes;
            }
        }
    }
}
