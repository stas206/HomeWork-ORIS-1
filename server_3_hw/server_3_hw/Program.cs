using System;
using System.IO;
using System.Text.Json;
using CustomHttpServer;
using server_3_hw;

// Читаем настройки из settings.json
Settings settings = new Settings();
if (File.Exists("settings.json"))
{
    string settingsJson = File.ReadAllText("settings.json");
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    settings = JsonSerializer.Deserialize<Settings>(settingsJson, options) ?? new Settings();
}
else
{
    Console.WriteLine("⚠️ Файл settings.json не найден! Используются настройки по умолчанию.");
}

// Создаём и запускаем сервер
HttpServer server = new HttpServer(settings);
server.Start();

Console.WriteLine("Введите 'stop' для остановки сервера:");

// Цикл чтения команд из консоли
while (true)
{
    string? command = Console.ReadLine();
    if (command != null && command.Trim().Equals("stop", StringComparison.OrdinalIgnoreCase))
    {
        server.Stop();
        break;
    }
}

Console.WriteLine("Программа завершена.");