using System.Reflection;
using CustomHttpServer.Framework;
using CustomHttpServer.Framework.Configuration;
using CustomHttpServer.Framework.Handlers;
using CustomHttpServer.Framework.Routing;
using CustomHttpServer.Services;

var settings = ConfigurationManager.Instance.Settings;

var controllerFactory = new DefaultControllerFactory();
var emailService = new EmailService(settings.Email);
controllerFactory.Register<IEmailService>(() => emailService);

var staticHandler = new StaticFilesHandler(
    settings.GetStaticRoot(), settings.AllowedSites, settings.Server.Path);
var controllersHandler = new ControllersHandler(
    Assembly.GetExecutingAssembly(), controllerFactory, settings.Server.Path);
var notFoundHandler = new NotFoundHandler();

staticHandler.SetNext(controllersHandler);
controllersHandler.SetNext(notFoundHandler);

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