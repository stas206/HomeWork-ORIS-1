using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CustomHttpServer;
using CustomHttpServer.Framework;

namespace server_3_hw;

public class HttpServer
{
    private HttpListener _server;
    private bool _isListening;
    private Dictionary<string, string> _mimeTypes;
    private Settings _settings;
    private readonly string _sitesFolder;
    private readonly string _htmlFolder;
    private Router _router;

    // Маппинг: имя сайта → имя главного файла
    private readonly Dictionary<string, string> _defaultFiles = new()
    {
        { "bonx", "index.html" },
        { "satisfactory-wiki", "index.html" },
        { "search", "search.html" },
        { "steam", "steam.html" }
    };

    public HttpServer(Settings settings)
    {
        _settings = settings;
        _sitesFolder = Path.Combine(Directory.GetCurrentDirectory(), settings.Folders.SitesFolder);
        _htmlFolder = Path.Combine(Directory.GetCurrentDirectory(), settings.Folders.HtmlFolder);
        _router = new Router();

        _server = new HttpListener();
        _server.Prefixes.Add($"http://{settings.Server.Host}:{settings.Server.Port}/{settings.Server.Path}");
    }

    public void Start()
    {
        // Загружаем MIME-типы
        if (File.Exists("types.json"))
        {
            string typesJson = File.ReadAllText("types.json");
            _mimeTypes = JsonSerializer.Deserialize<Dictionary<string, string>>(typesJson) 
                         ?? new Dictionary<string, string>();
        }
        else
        {
            _mimeTypes = new Dictionary<string, string>();
        }

        // Регистрируем контроллеры
        if (_settings.Controllers.Enabled)
        {
            _router.RegisterControllers(_htmlFolder);
        }

        _server.Start();
        _isListening = true;
        Console.WriteLine($"✅ Сервер запущен: http://{_settings.Server.Host}:{_settings.Server.Port}/");
        Console.WriteLine($"   Доступные сайты: {string.Join(", ", _settings.AllowedSites)}");

        Listen();
    }

    private string GetContentType(string extension)
    {
        if (_mimeTypes != null && _mimeTypes.TryGetValue(extension.ToLower(), out string contentType))
        {
            return contentType;
        }
        return "application/octet-stream";
    }

    private async void Listen()
    {
        while (_isListening)
        {
            try
            {
                var context = await _server.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                string localPath = request.Url.LocalPath.TrimStart('/');
                string[] parts = localPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 0)
                {
                    await Send404(response, "Путь не указан");
                    continue;
                }

                string firstPart = parts[0];

                // Проверяем, это запрос к контроллеру?
                if (_settings.Controllers.Enabled && IsControllerRequest(firstPart))
                {
                    await HandleControllerRequest(context, parts);
                }
                else
                {
                    await HandleStaticFileRequest(context, parts);
                }
            }
            catch (HttpListenerException)
            {
                // Ожидаемое исключение при остановке сервера
            }
        }
    }

    private bool IsControllerRequest(string firstPart)
    {
        return _router.HasController(firstPart);
    }

    private async Task HandleControllerRequest(HttpListenerContext context, string[] parts)
    {
        var response = context.Response;
        string controllerName = parts[0];
        string actionPath = parts.Length > 1 ? parts[1] : "";
        var queryParams = ParseQueryString(context.Request.Url.Query);

        var route = _router.FindRoute(controllerName, actionPath);

        if (route == null)
        {
            await Send404(response, $"Метод '{actionPath}' не найден в контроллере '{controllerName}'");
            Console.WriteLine($"404: /{controllerName}/{actionPath}");
            return;
        }

        try
        {
            string result = _router.ExecuteRoute(route, queryParams);
            response.StatusCode = 200;
            response.ContentType = "text/html; charset=utf-8";
            byte[] controllerBuffer = Encoding.UTF8.GetBytes(result);
            response.ContentLength64 = controllerBuffer.Length;
            await response.OutputStream.WriteAsync(controllerBuffer);
            Console.WriteLine($"200: /{controllerName}/{actionPath}");
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            response.ContentType = "text/html; charset=utf-8";
            string html = $"<h1>500 - Internal Server Error</h1><p>{ex.Message}</p>";
            byte[] errorBuffer = Encoding.UTF8.GetBytes(html);
            response.ContentLength64 = errorBuffer.Length;
            await response.OutputStream.WriteAsync(errorBuffer);
            Console.WriteLine($"500: {ex.Message}");
        }
    }

    private async Task HandleStaticFileRequest(HttpListenerContext context, string[] parts)
    {
        var response = context.Response;
        string siteName = parts[0];
        string fileInsideSite = parts.Length > 1 ? string.Join("/", parts.Skip(1)) : "";

        // Проверяем, есть ли такой сайт в списке разрешённых
        if (string.IsNullOrEmpty(siteName) || !_settings.AllowedSites.Contains(siteName))
        {
            await Send404(response, $"Сайт '{siteName}' не найден. Доступны: {string.Join(", ", _settings.AllowedSites)}");
            Console.WriteLine($"404: сайт '{siteName}' не найден");
            return;
        }

        // Если файл не указан — подставляем главный файл для этого сайта
        if (string.IsNullOrEmpty(fileInsideSite))
        {
            if (_defaultFiles.TryGetValue(siteName, out string defaultFile))
            {
                fileInsideSite = defaultFile;
            }
            else
            {
                fileInsideSite = "index.html";
            }
        }

        // Формируем полный путь: sites/имя_сайта/файл
        string filePath = Path.Combine(_sitesFolder, siteName, fileInsideSite);

        FileInfo fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            // Файл не найден — отдаём общую 404.html из папки sites
            response.StatusCode = 404;
            string errorPath = Path.Combine(_sitesFolder, "404.html");

            if (File.Exists(errorPath))
            {
                response.ContentType = GetContentType(".html");
                byte[] errorBuffer = await File.ReadAllBytesAsync(errorPath);
                response.ContentLength64 = errorBuffer.Length;
                await using Stream errorOutput = response.OutputStream;
                await errorOutput.WriteAsync(errorBuffer);
                await errorOutput.FlushAsync();
            }
            else
            {
                response.ContentType = "text/html; charset=utf-8";
                string html = $"<h1>404</h1><p>Файл '{fileInsideSite}' не найден на сайте '{siteName}'</p>";
                byte[] errorBuffer = Encoding.UTF8.GetBytes(html);
                response.ContentLength64 = errorBuffer.Length;
                await using Stream errorOutput = response.OutputStream;
                await errorOutput.WriteAsync(errorBuffer);
                await errorOutput.FlushAsync();
            }

            Console.WriteLine($"404: файл '{fileInsideSite}' не найден на сайте '{siteName}'");
            return;
        }

        // Файл найден — отдаём его
        response.ContentType = GetContentType(fileInfo.Extension);
        byte[] fileBuffer = await File.ReadAllBytesAsync(filePath);
        response.ContentLength64 = fileBuffer.Length;
        await using Stream fileOutput = response.OutputStream;
        await fileOutput.WriteAsync(fileBuffer);
        await fileOutput.FlushAsync();

        Console.WriteLine($"200: {siteName}/{fileInsideSite}");
    }

    private async Task Send404(HttpListenerResponse response, string message)
    {
        response.StatusCode = 404;
        response.ContentType = "text/html; charset=utf-8";

        string errorPath = Path.Combine(_sitesFolder, "404.html");
        if (File.Exists(errorPath))
        {
            byte[] errorBuffer = await File.ReadAllBytesAsync(errorPath);
            response.ContentLength64 = errorBuffer.Length;
            await using Stream errorOutput = response.OutputStream;
            await errorOutput.WriteAsync(errorBuffer);
            await errorOutput.FlushAsync();
        }
        else
        {
            string html = $"<h1>404</h1><p>{message}</p>";
            byte[] errorBuffer = Encoding.UTF8.GetBytes(html);
            response.ContentLength64 = errorBuffer.Length;
            await using Stream errorOutput = response.OutputStream;
            await errorOutput.WriteAsync(errorBuffer);
            await errorOutput.FlushAsync();
        }
    }

    private Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query)) return result;

        query = query.TrimStart('?');
        var pairs = query.Split('&');
        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=');
            if (keyValue.Length == 2)
            {
                result[keyValue[0]] = Uri.UnescapeDataString(keyValue[1]);
            }
        }
        return result;
    }

    public void Stop()
    {
        _isListening = false;
        _server.Stop();
        Console.WriteLine(" Сервер выключен");
    }
}