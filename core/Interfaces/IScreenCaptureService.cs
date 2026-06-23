using System.Threading.Tasks;

namespace Alissa.Core.Interfaces
{
    /// <summary>
    /// Service for capturing screen content for vision analysis.
    /// 
    /// PHASE 3 STUB - Not yet implemented.
    /// When implemented, this will:
    /// - Capture the current screen or window
    /// - Return raw image bytes
    /// - Support optional regions/selections
    /// 
    /// Decision needed: Which screen capture library to use? DirectX? Windows.Graphics.Capture?
    /// </summary>
    public interface IScreenCaptureService
    {
        /// <summary>
        /// Captures the current screen and returns raw image bytes.
        /// </summary>
        /// <param name="format">Image format (e.g., "png", "jpg")</param>
        /// <returns>Raw image data as byte array</returns>
        Task<byte[]> CaptureScreenAsync(string format = "png");

        /// <summary>
        /// Captures a specific region of the screen.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="width">Region width</param>
        /// <param name="height">Region height</param>
        /// <param name="format">Image format</param>
        /// <returns>Raw image data as byte array</returns>
        Task<byte[]> CaptureRegionAsync(int x, int y, int width, int height, string format = "png");
    }
}
