namespace MiniHttpServer.Framework.Configuration;

public sealed class ServerSettings
{
    public ServerConfig Server { get; set; } = new();
    public List<string> AllowedSites { get; set; } = new();
    public FoldersConfig Folders { get; set; } = new();
    public EmailSettings Email { get; set; } = new();
    public string ConnectionString { get; set; } = string.Empty;

    public string GetStaticRoot()
    {
        var folder = string.IsNullOrWhiteSpace(Folders.StaticFolder) ? "static" : Folders.StaticFolder;
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, folder));
    }
}

public sealed class ServerConfig
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8888;
    public string Path { get; set; } = "";
}

public sealed class FoldersConfig
{
    public string StaticFolder { get; set; } = "static";
}
