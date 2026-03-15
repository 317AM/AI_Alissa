using System;
using System.Collections.Generic;

namespace Alissa.Core.Models
{
    public class Session
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public List<Message> Messages { get; set; } = new List<Message>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public string LatestEmoji { get; set; } = string.Empty;
        public string CollectedEmojis { get; set; } = string.Empty;

        public void AddMessage(MessageRole role, string content)
        {
            Message newMessage = new Message(role, content);
            Messages.Add(newMessage);
            LastUpdated = DateTime.Now;
        }

        public int Length => Messages.Count;

        public override string ToString()
        {
            return $"Session {SessionId} ({Messages.Count} messages, created {CreatedAt})";
        }
    }
}
