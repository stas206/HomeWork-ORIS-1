using System.Net;

namespace MiniHttpServer.Framework.Handlers;

public sealed class LoggingHandler : RequestHandler
{
    public override Task HandleAsync(HttpListenerContext context)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context.Request.HttpMethod} {context.Request.Url?.AbsolutePath}");
        return HandleNextAsync(context);
    }
}
