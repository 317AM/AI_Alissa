namespace Alissa.Core.Interfaces
{
    /// <summary>
    /// Interface for chat clients that can capture and expose thinking/reasoning text.
    /// Models like qwen3:14b can emit internal reasoning before their response.
    /// </summary>
    public interface IThinkingCapable
    {
        /// <summary>
        /// Gets the last captured thinking/reasoning text.
        /// Returns empty string if no thinking was captured in the last exchange.
        /// </summary>
        string LastThinkingText { get; }

        /// <summary>
        /// Clears the captured thinking text for the next exchange.
        /// </summary>
        void ClearThinkingBuffer();
    }
}
