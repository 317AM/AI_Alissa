using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Alissa.Core.Interfaces
{
    /// <summary>
    /// Service for managing command execution and system integration.
    /// Allows Alissa to receive and execute structured commands from other systems.
    /// </summary>
    public interface ICommandService
    {
        /// <summary>
        /// Defines a command that Alissa can execute.
        /// </summary>
        Task RegisterCommandAsync(string name, Func<Dictionary<string, object>, Task<string>> handler);

        /// <summary>
        /// Executes a registered command with the given parameters.
        /// </summary>
        Task<string> ExecuteCommandAsync(string name, Dictionary<string, object> parameters);

        /// <summary>
        /// Gets all registered commands.
        /// </summary>
        Task<List<string>> GetAvailableCommandsAsync();

        /// <summary>
        /// Checks if a command is registered.
        /// </summary>
        Task<bool> HasCommandAsync(string name);

        /// <summary>
        /// Unregisters a command.
        /// </summary>
        Task UnregisterCommandAsync(string name);
    }
}
