namespace CustomHttpServer;

public class Settings
{
    public ServerConfig Server { get; set; } = new();
    public List<string> AllowedSites { get; set; } = new List<string>();
    public FoldersConfig Folders { get; set; } = new();
    public ControllersConfig Controllers { get; set; } = new();
}

public class ServerConfig
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8888;
    public string Path { get; set; } = "";
}

public class FoldersConfig
{
    public string SitesFolder { get; set; } = "sites";
    public string HtmlFolder { get; set; } = "html";
}

public class ControllersConfig
{
    public bool Enabled { get; set; } = true;
}