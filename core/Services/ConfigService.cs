using Alissa.Core.Models;
using System.Text.Json;

public static class ConfigService
{
    /// <summary>
    /// Loads all configuration files and merges them into a single AppConfig object.
    /// </summary>
    /// <param name="basePath">Base directory path containing config folder</param>
    /// <returns>Fully populated AppConfig object</returns>
    public static AppConfig LoadAll(string basePath)
    {
        string configDir = Path.Combine(basePath, "config");

        T Load<T>(string file) where T : new()
        {
            string path = Path.Combine(configDir, file);

            if (!File.Exists(path))
            {
                return new T();
            }

            try
            {
                string json = File.ReadAllText(path);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return new T();
                }

                T? obj = JsonSerializer.Deserialize<T>(json);
                return obj ?? new T();
            }
            catch
            {
                return new T();
            }
        }

        T LoadRequired<T>(string file)
        {
            string path = Path.Combine(configDir, file);

            if (!File.Exists(path))
                throw new Exception($"{file} missing.");

            string json = File.ReadAllText(path);

            if (string.IsNullOrWhiteSpace(json))
                throw new Exception($"{file} empty.");

            T? obj = JsonSerializer.Deserialize<T>(json);

            if (obj == null)
                throw new Exception($"{file} invalid.");

            return obj;
        }

        return new AppConfig
        {
            Model = LoadRequired<ConfigModel>("model.json"),
            Settings = LoadRequired<SettingsModel>("settings.json"),
            Limits = LoadRequired<LimitsModel>("limits.json"),
            Memory = LoadRequired<MemoryModel>("memory_rules.json"),
            PromptRules = Load<PromptRulesModel>("prompt_rules.json"),
            PersonalityRules = Load<PersonalityRulesModel>("personality_rules.json"),
            IndexingRules = Load<IndexingRulesModel>("indexing_rules.json"),
            Logging = Load<LoggingModel>("logging.json")
        };
    }
}
