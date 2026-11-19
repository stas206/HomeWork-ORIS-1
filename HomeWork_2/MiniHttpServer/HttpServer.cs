using System.Net;
using System.Text;

public class HttpServer
{
    private readonly HttpListener listener;
    private bool running;

    public HttpServer(Settings config)
    {
        listener = new HttpListener();

        string address =
            $"http://{config.Server.Host}:{config.Server.Port}/{config.Server.Path}";

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
            try
            {
                HttpListenerContext request =
                    await listener.GetContextAsync();

                await ProcessRequest(request);
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }

        Console.WriteLine("HTTP-сервер остановлен.");
    }

    private async Task ProcessRequest(HttpListenerContext context)
    {
        HttpListenerResponse response = context.Response;

        if (!File.Exists("search.html"))
        {
            string message = "Страница search.html не найдена.";
            byte[] data = Encoding.UTF8.GetBytes(message);

            response.StatusCode = 404;
            response.ContentType = "text/plain; charset=utf-8";
            response.ContentLength64 = data.Length;

            await response.OutputStream.WriteAsync(data);
            response.Close();

            return;
        }

        string page = await File.ReadAllTextAsync("search.html");
        byte[] content = Encoding.UTF8.GetBytes(page);

        response.StatusCode = 200;
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = content.Length;

        await response.OutputStream.WriteAsync(content);
        response.Close();

        Console.WriteLine("Получен HTTP-запрос.");
    }

    public void Stop()
    {
        if (!running)
            return;

        running = false;
        listener.Stop();

        Console.WriteLine("Остановка сервера...");
    }
}