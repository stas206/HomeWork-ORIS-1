namespace MyHttpServer;

public class Settings
{
    public Server Server { get; set; } = new();

    public List<string> AllowedSites { get; set; } = new();

    public string PublicDirectoryPath { get; set; } = "static";
}

public class Server
{
    public string Port { get; set; } = "9999";

    public string Host { get; set; } = "127.0.0.1";

    public string Path { get; set; } = "connection/";
}