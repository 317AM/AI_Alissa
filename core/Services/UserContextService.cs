using Alissa.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Alissa.Core.Services
{
    /// <summary>
    /// Service for managing user context and overrides.
    /// </summary>
    public class UserContextService : IUserContextService
    {
        private readonly string _basePath;
        private string? _manualUserOverride;
        private Dictionary<string, object> _userContext;

        public UserContextService(string basePath)
        {
            _basePath = basePath;
            _userContext = new Dictionary<string, object>();
            LoadUserContext();
        }

        public async Task<string> GetCurrentUserAsync()
        {
            bool hasManualOverride = _manualUserOverride != null;

            if (hasManualOverride)
            {
                return await Task.FromResult(_manualUserOverride!);
            }

            string detectedUser = DetectSystemUser();
            return await Task.FromResult(detectedUser);
        }

        public async Task SetManualUserAsync(string userName)
        {
            bool isValid = !string.IsNullOrWhiteSpace(userName);

            if (isValid)
            {
                _manualUserOverride = userName;
                _userContext["manual_override"] = true;
                _userContext["user_name"] = userName;
                await PersistUserContext();
            }

            await Task.CompletedTask;
        }

        public async Task ClearManualUserAsync()
        {
            _manualUserOverride = null;
            _userContext.Remove("manual_override");
            await PersistUserContext();
        }

        public async Task<Dictionary<string, object>> GetUserContextAsync()
        {
            return await Task.FromResult(new Dictionary<string, object>(_userContext));
        }

        public async Task SetUserContextAsync(Dictionary<string, object> context)
        {
            _userContext = new Dictionary<string, object>(context);
            await PersistUserContext();
        }

        public async Task<bool> HasManualUserOverrideAsync()
        {
            bool hasOverride = _manualUserOverride != null;
            return await Task.FromResult(hasOverride);
        }

        private string DetectSystemUser()
        {
            string? userName = Environment.UserName;
            string defaultName = !string.IsNullOrEmpty(userName) ? userName : "User";
            return defaultName;
        }

        private void LoadUserContext()
        {
            string contextPath = Path.Combine(_basePath, "config", "user_context.json");
            bool fileExists = File.Exists(contextPath);

            if (fileExists)
            {
                try
                {
                    string json = File.ReadAllText(contextPath);
                    var loaded = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    if (loaded != null)
                    {
                        _userContext = loaded;

                        bool hasManualOverride = _userContext.ContainsKey("user_name") && _userContext.ContainsKey("manual_override");
                        if (hasManualOverride)
                        {
                            _manualUserOverride = _userContext["user_name"]?.ToString();
                        }
                    }
                }
                catch
                {
                    // Use default context on error
                }
            }
        }

        private async Task PersistUserContext()
        {
            string contextPath = Path.Combine(_basePath, "config", "user_context.json");
            Directory.CreateDirectory(Path.GetDirectoryName(contextPath)!);

            string json = System.Text.Json.JsonSerializer.Serialize(_userContext, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(contextPath, json);
        }
    }
}
