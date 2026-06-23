using Alissa.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace Alissa.Core.Services
{
    /// <summary>
    /// PHASE 3 STUB - Screen capture service not yet implemented.
    /// 
    /// TODO: Implement actual screen capture using:
    /// - DirectX / Direct3D for hardware acceleration
    /// - Windows.Graphics.Capture API for modern Windows
    /// - Or GDI+ for compatibility
    /// 
    /// Current behavior: throws NotImplementedException with guidance.
    /// </summary>
    public class ScreenCaptureService : IScreenCaptureService
    {
        public Task<byte[]> CaptureScreenAsync(string format = "png")
        {
            var result = HandleNotImplemented("CaptureScreenAsync");
            return Task.FromResult(result);
        }

        public Task<byte[]> CaptureRegionAsync(int x, int y, int width, int height, string format = "png")
        {
            var result = HandleNotImplemented("CaptureRegionAsync");
            return Task.FromResult(result);
        }

        private byte[] HandleNotImplemented(string methodName)
        {
            var message = $"Screen capture ({methodName}) not yet implemented. " +
                         "To implement: Choose a screen capture library (DirectX, Windows.Graphics.Capture, GDI+) " +
                         "and implement the capture logic here.";

            System.Diagnostics.Debug.WriteLine(message);
            return Array.Empty<byte>();
        }
    }
}
