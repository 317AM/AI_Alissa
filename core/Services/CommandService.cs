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
        // Constants
        private const string COMMAND_NOT_FOUND = "Command '{0}' not found";

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
            return;
        }

        public async Task<string> ExecuteCommandAsync(string name, Dictionary<string, object> parameters)
        {
            bool hasCommand = _commands.ContainsKey(name);

            if (hasCommand)
            {
                Func<Dictionary<string, object>, Task<string>> handler = _commands[name];
                string result = await handler(parameters);
                return result;
            }

            string errorMessage = string.Format(COMMAND_NOT_FOUND, name);
            string finalResult = await Task.FromResult(errorMessage);
            return finalResult;
        }

        public async Task<List<string>> GetAvailableCommandsAsync()
        {
            List<string> commands = new List<string>(_commands.Keys);
            List<string> result = await Task.FromResult(commands);
            return result;
        }

        public async Task<bool> HasCommandAsync(string name)
        {
            bool hasCommand = _commands.ContainsKey(name);
            bool result = await Task.FromResult(hasCommand);
            return result;
        }

        public async Task UnregisterCommandAsync(string name)
        {
            bool hasCommand = _commands.ContainsKey(name);

            if (hasCommand)
            {
                _commands.Remove(name);
            }

            await Task.CompletedTask;
            return;
        }
    }
}
