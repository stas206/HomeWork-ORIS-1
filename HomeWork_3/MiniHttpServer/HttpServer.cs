using System.Net;
using System.Text;

namespace MyHttpServer;

public class HttpServer
{
    private readonly HttpListener listener;
    private readonly Settings config;
    private readonly string sitesFolder;
    private readonly MimeTypeResolver mimeResolver;

    private bool running;

    public HttpServer(Settings config)
    {
        this.config = config;

        listener = new HttpListener();

        mimeResolver = new MimeTypeResolver();

        sitesFolder = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                config.PublicDirectoryPath));

        string address =
            $"http://{config.Server.Host}:{config.Server.Port}/" +
            $"{config.Server.Path.Trim('/')}/";

        listener.Prefixes.Add(address);
    }

    public async Task StartAsync()
    {
        listener.Start();

        running = true;

        Console.WriteLine("HTTP-сервер запущен.");

        foreach (string address in listener.Prefixes)
        {
            Console.WriteLine($"Адрес: {address}");
        }

        while (running)
        {
            HttpListenerContext context;

            try
            {
                context = await listener.GetContextAsync();
            }
            catch (HttpListenerException) when (!running)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            try
            {
                await ProcessRequest(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка запроса: {ex.Message}");

                await SendError(
                    context.Response,
                    500,
                    "Внутренняя ошибка сервера.");
            }
        }

        Console.WriteLine("HTTP-сервер остановлен.");
    }

    private async Task ProcessRequest(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        response.Headers["Server"] = "MiniHttpServer/1.0";

        if (request.HttpMethod != "GET")
        {
            await SendError(
                response,
                405,
                "Метод не поддерживается.");

            return;
        }

        string url = Uri.UnescapeDataString(
            request.Url?.AbsolutePath ?? "/");

        string prefix = "/" + config.Server.Path.Trim('/');

        if (prefix != "/")
        {
            if (!url.StartsWith(
                    prefix + "/",
                    StringComparison.OrdinalIgnoreCase))
            {
                await Send404(response, "Неверный адрес.");

                return;
            }

            url = url[prefix.Length..];
        }

        string[] parts = url.TrimStart('/').Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            await Send404(response, "Сайт не указан.");

            return;
        }

        string siteName = parts[0];

        if (!config.AllowedSites.Contains(
                siteName,
                StringComparer.OrdinalIgnoreCase))
        {
            await Send404(response, "Сайт не найден.");

            return;
        }

        string siteFolder = Path.GetFullPath(
            Path.Combine(sitesFolder, siteName));

        string relativePath = string.Join(
            Path.DirectorySeparatorChar,
            parts.Skip(1));

        if (url.EndsWith("/") || string.IsNullOrEmpty(relativePath))
        {
            relativePath = Path.Combine(
                relativePath,
                "index.html");
        }

        string filePath = Path.GetFullPath(
            Path.Combine(siteFolder, relativePath));

        // Проверка безопасности пути.
        string safeRoot = siteFolder.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!filePath.StartsWith(
                safeRoot,
                StringComparison.OrdinalIgnoreCase))
        {
            await Send404(response, "Доступ запрещён.");

            return;
        }

        if (!File.Exists(filePath))
        {
            await Send404(response, "Файл не найден.");

            return;
        }

        FileInfo fileInfo = new FileInfo(filePath);

        response.StatusCode = 200;

        response.ContentType =
            mimeResolver.GetContentType(filePath);

        response.ContentLength64 = fileInfo.Length;

        // Передача файла через поток.
        using FileStream fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            true);

        await fileStream.CopyToAsync(response.OutputStream);

        response.Close();

        Console.WriteLine(
            $"200 OK: {siteName}/{relativePath}");
    }

    private async Task Send404(
        HttpListenerResponse response,
        string message)
    {
        response.StatusCode = 404;

        response.Headers["Server"] = "MiniHttpServer/1.0";

        string errorFile = Path.Combine(
            sitesFolder,
            "404.html");

        if (File.Exists(errorFile))
        {
            FileInfo info = new FileInfo(errorFile);

            response.ContentType =
                mimeResolver.GetContentType(errorFile);

            response.ContentLength64 = info.Length;

            using FileStream stream = new FileStream(
                errorFile,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                true);

            await stream.CopyToAsync(response.OutputStream);

            response.Close();

            return;
        }

        string html =
            $"<!DOCTYPE html><html lang=\"ru\">" +
            $"<meta charset=\"utf-8\">" +
            $"<h1>404 Not Found</h1>" +
            $"<p>{WebUtility.HtmlEncode(message)}</p>" +
            $"</html>";

        await SendText(response, 404, html);
    }

    private async Task SendError(
        HttpListenerResponse response,
        int statusCode,
        string message)
    {
        string html =
            $"<!DOCTYPE html><html lang=\"ru\">" +
            $"<meta charset=\"utf-8\">" +
            $"<h1>{statusCode}</h1>" +
            $"<p>{WebUtility.HtmlEncode(message)}</p>" +
            $"</html>";

        await SendText(response, statusCode, html);
    }

    private async Task SendText(
        HttpListenerResponse response,
        int statusCode,
        string text)
    {
        byte[] data = Encoding.UTF8.GetBytes(text);

        response.StatusCode = statusCode;

        response.ContentType = "text/html; charset=utf-8";

        response.ContentLength64 = data.Length;

        response.Headers["Server"] = "MiniHttpServer/1.0";

        await response.OutputStream.WriteAsync(data);

        response.Close();
    }

    public void Stop()
    {
        if (!running)
            return;

        running = false;

        listener.Stop();

        listener.Close();

        Console.WriteLine("Остановка сервера...");
    }
}