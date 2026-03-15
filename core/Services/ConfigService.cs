using Alissa.Core.Models;
using System.Text.Json;

public static class ConfigService
{
    public static AppConfig LoadAll(string basePath)
    {
        string configDir = Path.Combine(basePath, "config");

        T Load<T>(string file)
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
            Model = Load<ConfigModel>("model.json"),
            Settings = Load<SettingsModel>("settings.json"),
            Limits = Load<LimitsModel>("limits.json"),
            Memory = Load<MemoryModel>("memory_rules.json")
        };
    }
}
