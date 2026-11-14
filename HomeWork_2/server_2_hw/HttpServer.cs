using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyHttpServer;

public class HttpServer
{
    private HttpListener server;
    private bool _isListening;
    public Dictionary<string, string> _mimeTypes;
    private Settings _settings;
    private readonly string _sitesFolder;

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
        _sitesFolder = Path.Combine(@"C:\Users\user\RiderProjects\server_2_hw\server_2_hw", "sites");
        server = new HttpListener();
        server.Prefixes.Add($"http://{settings.Server.Host}:{settings.Server.Port}/{settings.Server.Path}");
    }

    public void Start()
    {
        server.Start();
        _isListening = true;
        Console.WriteLine("Сервер запущен");

        if (File.Exists("types.json"))
        {
            string typesJson = File.ReadAllText("types.json");
            _mimeTypes = JsonSerializer.Deserialize<Dictionary<string, string>>(typesJson);
        }

        Listen();
    }

    private string GetContentType(string extension)
    {
        if (_mimeTypes != null && _mimeTypes.TryGetValue(extension.ToLower(), out string contentType))
        {
            return contentType;
        }
        else return "text/html";
    }

    private async void Listen()
    {
        while (_isListening)
        {
            try
            {
                var context = await server.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                string localPath = request.Url.LocalPath.TrimStart('/');

                string[] parts = localPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                
                string siteName = parts.Length > 0 ? parts[0] : "";
                
                string fileInsideSite = parts.Length > 1 ? string.Join("/", parts.Skip(1)) : "";
                
                if (string.IsNullOrEmpty(siteName) || !_settings.AllowedSites.Contains(siteName))
                {
                    await Send404(response, $"Сайт '{siteName}' не найден. Доступны: {string.Join(", ", _settings.AllowedSites)}");
                    Console.WriteLine($"404: сайт '{siteName}' не найден");
                    continue;
                }
                
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

                string filePath = Path.Combine(_sitesFolder, siteName, fileInsideSite);

                FileInfo fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    response.StatusCode = 404;
                    string errorPath = Path.Combine(_sitesFolder, "404.html");

                    if (File.Exists(errorPath))
                    {
                        response.ContentType = GetContentType(".html");
                        byte[] buffer = await File.ReadAllBytesAsync(errorPath);
                        response.ContentLength64 = buffer.Length;
                        using Stream output = response.OutputStream;
                        await output.WriteAsync(buffer);
                        await output.FlushAsync();
                    }
                    else
                    {
                        response.ContentType = "text/html; charset=utf-8";
                        string html = $"<h1>404</h1><p>Файл '{fileInsideSite}' не найден на сайте '{siteName}'</p>";
                        byte[] buffer = Encoding.UTF8.GetBytes(html);
                        response.ContentLength64 = buffer.Length;
                        using Stream output = response.OutputStream;
                        await output.WriteAsync(buffer);
                        await output.FlushAsync();
                    }

                    Console.WriteLine($"404: файл '{fileInsideSite}' не найден на сайте '{siteName}'");
                    continue;
                }
                
                response.ContentType = GetContentType(fileInfo.Extension);

                byte[] buffer2 = await File.ReadAllBytesAsync(filePath);
                response.ContentLength64 = buffer2.Length;

                using Stream output2 = response.OutputStream;
                await output2.WriteAsync(buffer2);
                await output2.FlushAsync();

                Console.WriteLine($"200: {siteName}/{fileInsideSite}");
            }
            catch (HttpListenerException)
            {
            }
        }
    }

    private async Task Send404(HttpListenerResponse response, string message)
    {
        response.StatusCode = 404;
        response.ContentType = "text/html; charset=utf-8";

        string errorPath = Path.Combine(_sitesFolder, "404.html");
        if (File.Exists(errorPath))
        {
            byte[] buffer = await File.ReadAllBytesAsync(errorPath);
            response.ContentLength64 = buffer.Length;
            using Stream output = response.OutputStream;
            await output.WriteAsync(buffer);
            await output.FlushAsync();
        }
        else
        {
            string html = $"<h1>404</h1><p>{message}</p>";
            byte[] buffer = Encoding.UTF8.GetBytes(html);
            response.ContentLength64 = buffer.Length;
            using Stream output = response.OutputStream;
            await output.WriteAsync(buffer);
            await output.FlushAsync();
        }
    }

    public void Stop()
    {
        _isListening = false;
        server.Stop();
        Console.WriteLine("Сервер выключен");
    }
}