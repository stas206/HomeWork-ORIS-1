using System.Net;
using CustomHttpServer.Framework.Configuration;
using CustomHttpServer.Framework.Handlers;

namespace CustomHttpServer.Framework;

public sealed class HttpServer
{
    private readonly HttpListener _listener = new();
    private readonly ServerSettings _settings;
    private readonly RequestHandler _firstHandler;
    private bool _isRunning;

    public HttpServer(ServerSettings settings, RequestHandler firstHandler)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _firstHandler = firstHandler ?? throw new ArgumentNullException(nameof(firstHandler));
        var host = string.IsNullOrWhiteSpace(settings.Server.Host) ? "127.0.0.1" : settings.Server.Host;
        var path = (settings.Server.Path ?? "").Trim('/');
        _listener.Prefixes.Add($"http://{host}:{settings.Server.Port}/{(path.Length == 0 ? "" : path + "/")}");
    }

    public async Task StartAsync()
    {
        _listener.Start();
        _isRunning = true;
        Console.WriteLine($"Сервер запущен: {_listener.Prefixes.First()}");

        while (_isRunning)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (HttpListenerException) when (!_isRunning) { break; }
            catch (ObjectDisposedException) when (!_isRunning) { break; }

            try
            {
                await _firstHandler.HandleAsync(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки запроса: {ex.Message}");
                try
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.Close();
                }
                catch {  }
            }
        }
    }

    public void Stop()
    {
        if (!_isRunning) return;
        _isRunning = false;
        _listener.Stop();
        _listener.Close();
        Console.WriteLine("Сервер остановлен.");
    }
}
