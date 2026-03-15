namespace Alissa.Core.Models
{
    public class ConfigModel
    {
        public string ModelName { get; set; } = null!;
        public int KeepAliveMinutes { get; set; }
        public int MaxTokens { get; set; }
        public int ResponseTimeoutSeconds { get; set; }
    }
}
