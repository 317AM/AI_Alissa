using Alissa.Core.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Alissa.Core.Services
{
    /// <summary>
    /// Helper service for reading and writing persona.json configuration.
    /// Manages current user and current code context that PromptBuilder injects into prompts.
    /// </summary>
    public static class PersonaService
    {
        private static readonly JsonSerializerOptions _json = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Updates the current_code section of persona.json with file, language, and task information.
        /// Called when /read, /analyze, or VS Extension sends code context.
        /// </summary>
        public static async Task UpdateCurrentCodeAsync(
            string basePath,
            string filePath,
            string language,
            string task)
        {
            try
            {
                string personaPath = Path.Combine(basePath, "config", "persona.json");
                PersonaModel? persona = null;

                if (File.Exists(personaPath))
                {
                    string json = await File.ReadAllTextAsync(personaPath);
                    persona = JsonSerializer.Deserialize<PersonaModel>(json, _json);
                }

                persona ??= new PersonaModel();
                persona.CurrentCode = new CodeContext
                {
                    Name = Path.GetFileName(filePath),
                    Language = language,
                    Task = task
                };

                string updatedJson = JsonSerializer.Serialize(persona, _json);
                await File.WriteAllTextAsync(personaPath, updatedJson);
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, null, false);
            }
        }

        /// <summary>
        /// Loads the current_code section from persona.json.
        /// </summary>
        public static async Task<CodeContext?> GetCurrentCodeAsync(string basePath)
        {
            try
            {
                string personaPath = Path.Combine(basePath, "config", "persona.json");

                if (!File.Exists(personaPath))
                {
                    return null;
                }

                string json = await File.ReadAllTextAsync(personaPath);
                var persona = JsonSerializer.Deserialize<PersonaModel>(json, _json);

                return persona?.CurrentCode;
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, null, false);
                return null;
            }
        }

        /// <summary>
        /// Updates the current_user section of persona.json.
        /// </summary>
        public static async Task UpdateCurrentUserAsync(string basePath, string userName)
        {
            try
            {
                string personaPath = Path.Combine(basePath, "config", "persona.json");
                PersonaModel? persona = null;

                if (File.Exists(personaPath))
                {
                    string json = await File.ReadAllTextAsync(personaPath);
                    persona = JsonSerializer.Deserialize<PersonaModel>(json, _json);
                }

                persona ??= new PersonaModel();
                persona.CurrentUser = new UserContext
                {
                    Name = userName
                };

                string updatedJson = JsonSerializer.Serialize(persona, _json);
                await File.WriteAllTextAsync(personaPath, updatedJson);
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, null, false);
            }
        }

        /// <summary>
        /// Loads the entire persona.json file.
        /// </summary>
        public static async Task<PersonaModel?> LoadPersonaAsync(string basePath)
        {
            try
            {
                string personaPath = Path.Combine(basePath, "config", "persona.json");

                if (!File.Exists(personaPath))
                {
                    return null;
                }

                string json = await File.ReadAllTextAsync(personaPath);
                return JsonSerializer.Deserialize<PersonaModel>(json, _json);
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, null, false);
                return null;
            }
        }
    }
}
