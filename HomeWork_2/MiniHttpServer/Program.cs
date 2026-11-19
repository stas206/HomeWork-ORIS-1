using System.Text.Json;

if (!File.Exists("settings.json"))
{
    Console.WriteLine("Не найден файл settings.json.");
    return;
}

string json = await File.ReadAllTextAsync("settings.json");

Settings? config = JsonSerializer.Deserialize<Settings>(json);

if (config is null)
{
    Console.WriteLine("Не удалось загрузить настройки.");
    return;
}

var httpServer = new HttpServer(config);

Task serverWork = httpServer.StartAsync();

Console.WriteLine("Для завершения работы введите: stop");

while (true)
{
    string? input = Console.ReadLine();

    if (string.Equals(input?.Trim(), "stop",
            StringComparison.OrdinalIgnoreCase))
    {
        httpServer.Stop();
        break;
    }
}

await serverWork;

Console.WriteLine("Работа программы завершена.");