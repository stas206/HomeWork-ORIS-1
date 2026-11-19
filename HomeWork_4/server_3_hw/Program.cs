using CustomHttpServer.Framework;
using CustomHttpServer.Framework.Configuration;
using CustomHttpServer.Framework.Handlers;

var settings = ConfigurationManager.Instance.Settings;
var staticHandler = new StaticFilesHandler(settings.GetStaticRoot(), settings.AllowedSites, settings.Server.Path);
var notFoundHandler = new NotFoundHandler();
staticHandler.SetNext(notFoundHandler);

var loggingHandler = new LoggingHandler();
loggingHandler.SetNext(staticHandler);

var server = new HttpServer(settings, loggingHandler);
var serverTask = server.StartAsync();
Console.WriteLine("Введите stop для остановки сервера.");
while (true)
{
    var command = Console.ReadLine();
    if (string.Equals(command?.Trim(), "stop", StringComparison.OrdinalIgnoreCase)) break;
}
server.Stop();
await serverTask;