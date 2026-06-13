using System.Collections.Generic;
using System.Threading.Tasks;

namespace Alissa.Core.Interfaces
{
    /// <summary>
    /// Service for managing user context and overrides.
    /// Allows manual setting of user information that overrides automatic detection.
    /// </summary>
    public interface IUserContextService
    {
        /// <summary>
        /// Gets the current user name. Returns manual override if set, otherwise automatic detection.
        /// </summary>
        Task<string> GetCurrentUserAsync();

        /// <summary>
        /// Manually sets the user name, overriding automatic detection.
        /// </summary>
        Task SetManualUserAsync(string userName);

        /// <summary>
        /// Clears manual user override and returns to automatic detection.
        /// </summary>
        Task ClearManualUserAsync();

        /// <summary>
        /// Gets the current user context (name, preferences, metadata).
        /// </summary>
        Task<Dictionary<string, object>> GetUserContextAsync();

        /// <summary>
        /// Sets user context metadata for personalization.
        /// </summary>
        Task SetUserContextAsync(Dictionary<string, object> context);

        /// <summary>
        /// Checks if user override is currently active.
        /// </summary>
        Task<bool> HasManualUserOverrideAsync();
    }
}
