using System;
using System.IO;
using System.Text.Json;
using CustomHttpServer;
using server_3_hw;

Settings settings = new Settings();
if (File.Exists("settings.json"))
{
    string settingsJson = File.ReadAllText("settings.json");
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    settings = JsonSerializer.Deserialize<Settings>(settingsJson, options) ?? new Settings();
}

HttpServer server = new HttpServer(settings);
server.Start();

while (true)
{
    string? command = Console.ReadLine();
    if (command != null && command.Trim().Equals("stop", StringComparison.OrdinalIgnoreCase))
    {
        server.Stop();
        break;
    }
}