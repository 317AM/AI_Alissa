using System;

namespace Alissa.Core.Models
{
    public enum MessageRole
    {
        User,
        AI,
        System
    }

    public class Message
    {
        public MessageRole Role { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public Message() { }

        public Message(MessageRole role, string content)
        {
            Role = role;
            Content = content;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm}] {Role}: {Content}";
        }
    }
}
