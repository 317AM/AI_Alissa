using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alissa.Core.Models
{
    /// <summary>
    /// Configuration for the local HTTP API.
    /// </summary>
    public class ApiConfig
    {
        /// <summary>
        /// Whether the API is enabled.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Port to listen on for the HTTP API.
        /// </summary>
        [JsonPropertyName("listenPort")]
        public int ListenPort { get; set; } = 31700;

        /// <summary>
        /// List of IP addresses allowed to connect to the API.
        /// Default: localhost only (127.0.0.1, ::1).
        /// </summary>
        [JsonPropertyName("allowedHosts")]
        public List<string> AllowedHosts { get; set; } = new() { "127.0.0.1", "::1" };

        /// <summary>
        /// Shared secret header value for authentication.
        /// If empty, no authentication is required (local-trust mode).
        /// If set, clients must send "X-Alissa-Secret" header matching this value.
        /// </summary>
        [JsonPropertyName("sharedSecretHeader")]
        public string SharedSecretHeader { get; set; } = string.Empty;

        /// <summary>
        /// Maximum request body size in bytes (default: 5 MB).
        /// </summary>
        [JsonPropertyName("maxRequestBodyBytes")]
        public long MaxRequestBodyBytes { get; set; } = 5242880;
    }
}
