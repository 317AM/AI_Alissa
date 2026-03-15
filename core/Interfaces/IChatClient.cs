namespace Alissa.Core.Interfaces
{
    public interface IChatClient
    {
        IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userInput);

        // New overload that forwards emojis via callback (optional)
        IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userInput, Action<string>? onEmoji = null);
    }
}
