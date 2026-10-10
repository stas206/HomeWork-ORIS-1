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

        if (_settings.Controllers.Enabled)
        {
            _router.RegisterControllers(_htmlFolder);
        }

        _server.Start();
        _isListening = true;
        Console.WriteLine($"Сервер запущен: http://{_settings.Server.Host}:{_settings.Server.Port}/");

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
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            response.ContentType = "text/html; charset=utf-8";
            string html = $"<h1>500</h1><p>{ex.Message}</p>";
            byte[] errorBuffer = Encoding.UTF8.GetBytes(html);
            response.ContentLength64 = errorBuffer.Length;
            await response.OutputStream.WriteAsync(errorBuffer);
        }
    }

    private async Task HandleStaticFileRequest(HttpListenerContext context, string[] parts)
    {
        var request = context.Request;
        var response = context.Response;
        string siteName = parts[0];
        string fileInsideSite = parts.Length > 1 ? string.Join("/", parts.Skip(1)) : "";

        if (string.IsNullOrEmpty(siteName) || !_settings.AllowedSites.Contains(siteName))
        {
            await Send404(response, $"Сайт '{siteName}' не найден");
            return;
        }

        if (IsMethodWithBody(request.HttpMethod) && request.HasEntityBody)
        {
            string rawBody = await ReadRawBody(request);
            Dictionary<string, string> formData = ParseFormData(rawBody);

            string login = formData.TryGetValue("login", out string l) ? l : "";
            string password = formData.TryGetValue("password", out string p) ? p : "";

            Console.WriteLine($"login = {login}");
            Console.WriteLine($"password = {password}");
        }

        if (string.IsNullOrEmpty(fileInsideSite))
        {
            fileInsideSite = _defaultFiles.TryGetValue(siteName, out string df) ? df : "index.html";
        }

        string filePath = Path.Combine(_sitesFolder, siteName, fileInsideSite);
        FileInfo fileInfo = new FileInfo(filePath);

        if (!fileInfo.Exists)
        {
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
                string html = $"<h1>404</h1><p>Файл не найден</p>";
                byte[] errorBuffer = Encoding.UTF8.GetBytes(html);
                response.ContentLength64 = errorBuffer.Length;
                await using Stream errorOutput = response.OutputStream;
                await errorOutput.WriteAsync(errorBuffer);
                await errorOutput.FlushAsync();
            }

            return;
        }

        response.ContentType = GetContentType(fileInfo.Extension);
        byte[] fileBuffer = await File.ReadAllBytesAsync(filePath);
        response.ContentLength64 = fileBuffer.Length;
        await using Stream fileOutput = response.OutputStream;
        await fileOutput.WriteAsync(fileBuffer);
        await fileOutput.FlushAsync();
    }

    private bool IsMethodWithBody(string method)
    {
        return method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            || method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
            || method.Equals("PATCH", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> ReadRawBody(HttpListenerRequest request)
    {
        using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private Dictionary<string, string> ParseFormData(string body)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(body)) return result;

        foreach (var pair in body.Split('&'))
        {
            if (string.IsNullOrEmpty(pair)) continue;
            var kv = pair.Split('=', 2);
            string key = DecodeFormValue(kv[0]);
            string value = kv.Length > 1 ? DecodeFormValue(kv[1]) : "";
            result[key] = value;
        }
        return result;
    }

    private string DecodeFormValue(string s)
    {
        return Uri.UnescapeDataString(s.Replace('+', ' '));
    }

    private Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query)) return result;

        query = query.TrimStart('?');
        foreach (var pair in query.Split('&'))
        {
            if (string.IsNullOrEmpty(pair)) continue;
            var kv = pair.Split('=', 2);
            string key = DecodeFormValue(kv[0]);
            string value = kv.Length > 1 ? DecodeFormValue(kv[1]) : "";
            result[key] = value;
        }
        return result;
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

    public void Stop()
    {
        _isListening = false;
        _server.Stop();
        Console.WriteLine("Сервер выключен");
    }
}