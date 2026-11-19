using System.Text.Json;

namespace MyHttpServer;

public sealed class ConfigurationManager
{
    private static readonly Lazy<ConfigurationManager> _instance =
        new(() => new ConfigurationManager());

    public static ConfigurationManager Instance => _instance.Value;

    public Settings Settings { get; }

    private ConfigurationManager()
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "settings.json");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                "Файл settings.json не найден.", path);
        }

        string json = File.ReadAllText(path);

        Settings = JsonSerializer.Deserialize<Settings>(
                       json,
                       new JsonSerializerOptions
                       {
                           PropertyNameCaseInsensitive = true
                       })
                   ?? throw new InvalidOperationException(
                       "Не удалось загрузить настройки.");
    }
}