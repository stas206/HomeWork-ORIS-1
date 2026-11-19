using MyHttpServer;

try
{
    ConfigurationManager configuration = ConfigurationManager.Instance;

    var httpServer = new HttpServer(configuration.Settings);

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
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
}