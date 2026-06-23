using System.Threading.Tasks;

namespace Alissa.Core.Interfaces
{
    /// <summary>
    /// Service for controlling a VTuber avatar based on Alissa's state and mood.
    /// 
    /// PHASE 3 STUB - Not yet implemented.
    /// When implemented, this will:
    /// - Read PersonaModel.Appearance.Mood
    /// - Read Session.LatestEmoji and Session.CollectedEmojis
    /// - Send control signals to a VTuber control API (e.g., VTube Studio WebSocket)
    /// - Animate expressions, moods, and reactions in real-time
    /// 
    /// Decision needed: Which VTuber control protocol? VTube Studio WebSocket? OBS? Custom?
    /// Assumption: Will connect to external service on startup, fire-and-forget on expression changes.
    /// </summary>
    public interface IVTuberBridgeService
    {
        /// <summary>
        /// Sets the VTuber's current mood/expression based on Alissa's state.
        /// </summary>
        /// <param name="mood">Mood string (e.g., "happy", "thinking", "annoyed", "excited")</param>
        /// <returns>Task</returns>
        Task SetMoodAsync(string mood);

        /// <summary>
        /// Triggers a specific expression or animation on the VTuber.
        /// </summary>
        /// <param name="expression">Expression identifier (e.g., "wink", "nod", "shake")</param>
        /// <returns>Task</returns>
        Task TriggerExpressionAsync(string expression);

        /// <summary>
        /// Checks if the bridge is connected to the VTuber control service.
        /// </summary>
        /// <returns>True if connected, false otherwise</returns>
        bool IsConnected { get; }

        /// <summary>
        /// Connects to the VTuber control service (e.g., VTube Studio).
        /// Should be called once at startup.
        /// </summary>
        /// <returns>Task</returns>
        Task ConnectAsync();

        /// <summary>
        /// Disconnects from the VTuber control service.
        /// </summary>
        /// <returns>Task</returns>
        Task DisconnectAsync();
    }
}
