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
        // Constants
        private const string CONFIG_DIR = "config";
        private const string USER_CONTEXT_FILE = "user_context.json";
        private const string MANUAL_OVERRIDE_KEY = "manual_override";
        private const string USER_NAME_KEY = "user_name";
        private const string DEFAULT_USER_NAME = "User";

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
                _userContext[MANUAL_OVERRIDE_KEY] = true;
                _userContext[USER_NAME_KEY] = userName;
                await PersistUserContext();
            }

            await Task.CompletedTask;
        }

        public async Task ClearManualUserAsync()
        {
            _manualUserOverride = null;
            _userContext.Remove(MANUAL_OVERRIDE_KEY);
            await PersistUserContext();
        }

        public async Task<Dictionary<string, object>> GetUserContextAsync()
        {
            Dictionary<string, object> contextCopy = new Dictionary<string, object>(_userContext);
            return await Task.FromResult(contextCopy);
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
            string result = !string.IsNullOrEmpty(userName) ? userName : DEFAULT_USER_NAME;
            return result;
        }

        private void LoadUserContext()
        {
            string contextPath = Path.Combine(_basePath, CONFIG_DIR, USER_CONTEXT_FILE);
            bool fileExists = File.Exists(contextPath);

            if (fileExists)
            {
                try
                {
                    string json = File.ReadAllText(contextPath);
                    Dictionary<string, object>? loaded = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    bool loadedValid = loaded != null;
                    if (loadedValid)
                    {
                        _userContext = loaded;

                        bool hasManualOverride = _userContext.ContainsKey(USER_NAME_KEY) && _userContext.ContainsKey(MANUAL_OVERRIDE_KEY);
                        if (hasManualOverride)
                        {
                            object? userNameObj = _userContext[USER_NAME_KEY];
                            _manualUserOverride = userNameObj?.ToString();
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
            string contextPath = Path.Combine(_basePath, CONFIG_DIR, USER_CONTEXT_FILE);
            string? directory = Path.GetDirectoryName(contextPath);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            System.Text.Json.JsonSerializerOptions options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            string json = System.Text.Json.JsonSerializer.Serialize(_userContext, options);
            await File.WriteAllTextAsync(contextPath, json);
        }
    }
}
