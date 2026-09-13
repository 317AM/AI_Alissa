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
        // Constants
        private const string CONFIG_DIR = "config";
        private const string PERSONA_FILE = "persona.json";

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
                string personaPath = Path.Combine(basePath, CONFIG_DIR, PERSONA_FILE);
                PersonaModel? persona = null;

                bool fileExists = File.Exists(personaPath);
                if (fileExists)
                {
                    string json = await File.ReadAllTextAsync(personaPath);
                    persona = JsonSerializer.Deserialize<PersonaModel>(json, _json);
                }

                bool hasPersona = persona != null;
                if (!hasPersona)
                {
                    persona = new PersonaModel();
                }

                string fileName = Path.GetFileName(filePath);
                persona.CurrentCode = new CodeContext
                {
                    Name = fileName,
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
                string personaPath = Path.Combine(basePath, CONFIG_DIR, PERSONA_FILE);

                bool fileExists = File.Exists(personaPath);
                if (!fileExists)
                {
                    return null;
                }

                string json = await File.ReadAllTextAsync(personaPath);
                PersonaModel? persona = JsonSerializer.Deserialize<PersonaModel>(json, _json);

                CodeContext? result = persona?.CurrentCode;
                return result;
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
                string personaPath = Path.Combine(basePath, CONFIG_DIR, PERSONA_FILE);
                PersonaModel? persona = null;

                bool fileExists = File.Exists(personaPath);
                if (fileExists)
                {
                    string json = await File.ReadAllTextAsync(personaPath);
                    persona = JsonSerializer.Deserialize<PersonaModel>(json, _json);
                }

                bool hasPersona = persona != null;
                if (!hasPersona)
                {
                    persona = new PersonaModel();
                }

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
                string personaPath = Path.Combine(basePath, CONFIG_DIR, PERSONA_FILE);

                bool fileExists = File.Exists(personaPath);
                if (!fileExists)
                {
                    return null;
                }

                string json = await File.ReadAllTextAsync(personaPath);
                PersonaModel? result = JsonSerializer.Deserialize<PersonaModel>(json, _json);
                return result;
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, null, false);
                return null;
            }
        }
    }
}
