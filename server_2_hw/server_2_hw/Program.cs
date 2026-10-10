using System;
using System.Net;
using System.Text;
using System.Text.Json;
using MyHttpServer;

Settings settings = new Settings();
if (File.Exists("settings.json"))
{
    string settingsJson = File.ReadAllText("settings.json");
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    settings = JsonSerializer.Deserialize<Settings>(settingsJson, options);
}

HttpServer server = new HttpServer(settings);
server.Start();

while (true)
{
    string? command = Console.ReadLine();
    if (command != null && command.Equals("stop"))
    {
        server.Stop();
        break;
    }
}