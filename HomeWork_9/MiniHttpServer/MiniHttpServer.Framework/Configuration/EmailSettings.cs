namespace MiniHttpServer.Framework.Configuration;

public sealed class EmailSettings
{
    public string Mode { get; set; } = "File";

    public string OutputFolder { get; set; } = "emails";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1025;

    public string From { get; set; } = "no-reply@example.com";

    public bool UseSsl { get; set; } = false;

    public string UserName { get; set; } = "";

    public string Password { get; set; } = "";
}
