using Alissa.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Alissa.Core.Services
{
    /// <summary>
    /// Service for managing command execution and system integration.
    /// </summary>
    public class CommandService : ICommandService
    {
        private readonly Dictionary<string, Func<Dictionary<string, object>, Task<string>>> _commands;

        public CommandService()
        {
            _commands = new Dictionary<string, Func<Dictionary<string, object>, Task<string>>>();
        }

        public async Task RegisterCommandAsync(string name, Func<Dictionary<string, object>, Task<string>> handler)
        {
            bool hasValidName = !string.IsNullOrWhiteSpace(name);
            bool hasValidHandler = handler != null;

            if (hasValidName && hasValidHandler)
            {
                _commands[name] = handler;
            }

            await Task.CompletedTask;
        }

        public async Task<string> ExecuteCommandAsync(string name, Dictionary<string, object> parameters)
        {
            bool hasCommand = _commands.ContainsKey(name);

            if (hasCommand)
            {
                var handler = _commands[name];
                string result = await handler(parameters);
                return result;
            }

            string errorMessage = $"Command '{name}' not found";
            return await Task.FromResult(errorMessage);
        }

        public async Task<List<string>> GetAvailableCommandsAsync()
        {
            var commands = new List<string>(_commands.Keys);
            return await Task.FromResult(commands);
        }

        public async Task<bool> HasCommandAsync(string name)
        {
            bool hasCommand = _commands.ContainsKey(name);
            return await Task.FromResult(hasCommand);
        }

        public async Task UnregisterCommandAsync(string name)
        {
            bool hasCommand = _commands.ContainsKey(name);

            if (hasCommand)
            {
                _commands.Remove(name);
            }

            await Task.CompletedTask;
        }
    }
}
