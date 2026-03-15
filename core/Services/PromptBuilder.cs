using Alissa.Core.Interfaces;
using Alissa.Core.Models;

public class PromptBuilder : IPromptBuilder
{
    private readonly string _basePath;
    private readonly IMemoryManager _memoryManager;

    public PromptBuilder(string basePath, IMemoryManager memoryManager)
    {
        _basePath = basePath;
        _memoryManager = memoryManager;
    }

    public string BuildSystemPrompt()
    {
        string identityFile = Path.Combine(_basePath, "personality", "identity.txt");
        string behaviorFile = Path.Combine(_basePath, "personality", "behaviour.txt");
        string boundariesFile = Path.Combine(_basePath, "personality", "boundaries.txt");

        string identity = File.Exists(identityFile) ? File.ReadAllText(identityFile) : "";
        string behavior = File.Exists(behaviorFile) ? File.ReadAllText(behaviorFile) : "";
        string boundaries = File.Exists(boundariesFile) ? File.ReadAllText(boundariesFile) : "";

        // Include top memories, filtered by importance
        var topMemory = _memoryManager.LoadTopMemories(10);
        string memoryPrompt = string.Empty;
        if (topMemory.Any())
        {
            memoryPrompt = "\n## Known Facts:\n" + string.Join("\n", topMemory.Select(m => $"- {m.Key}: {m.Value}"));
        }

        return $"{identity}\n{behavior}\n{boundaries}{memoryPrompt}";
    }

    public string BuildSystemPromptWithContext(List<Message> recentMessages)
    {
        string basePrompt = BuildSystemPrompt();

        if (!recentMessages.Any())
            return basePrompt;

        // Format recent conversation for context
        string conversationContext = "\n## Recent Conversation Context:\n";
        foreach (var msg in recentMessages.TakeLast(5))
        {
            string role = msg.Role == MessageRole.User ? "User" : "You";
            conversationContext += $"[{msg.Role}]: {msg.Content}\n";
        }

        return basePrompt + conversationContext;
    }
}
