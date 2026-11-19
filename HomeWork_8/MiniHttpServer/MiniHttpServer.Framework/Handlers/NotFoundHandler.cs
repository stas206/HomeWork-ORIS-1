using System.Net;
using System.Text;

namespace MiniHttpServer.Framework.Handlers;

public sealed class NotFoundHandler : RequestHandler
{
    public override async Task HandleAsync(HttpListenerContext context)
    {
        const string html = "<!DOCTYPE html><html lang=\"ru\"><head><meta charset=\"utf-8\"><title>404</title></head>" +
                            "<body><h1>404 - Not Found</h1><p>Запрашиваемый ресурс не найден.</p></body></html>";
        var bytes = Encoding.UTF8.GetBytes(html);
        var response = context.Response;
        response.StatusCode = (int)HttpStatusCode.NotFound;
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        response.Headers["Server"] = "MiniHttpServer";
        if (!context.Request.HttpMethod.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
            await response.OutputStream.WriteAsync(bytes);
        response.Close();
    }
}
