using System.Text.Json;

namespace CustomHttpServer.Framework.Configuration;

public sealed class ConfigurationManager
{
    private static readonly Lazy<ConfigurationManager> _instance = new(() => new ConfigurationManager());
    public static ConfigurationManager Instance => _instance.Value;
    public ServerSettings Settings { get; }

    private ConfigurationManager()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "settings.json");
        if (!File.Exists(path))
        {
            Settings = new ServerSettings();
            return;
        }

        var json = File.ReadAllText(path);
        Settings = JsonSerializer.Deserialize<ServerSettings>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ServerSettings();
    }
}