using System.Net;

namespace MiniHttpServer.Framework.Handlers;

public abstract class RequestHandler
{
    protected RequestHandler? Next { get; private set; }

    public void SetNext(RequestHandler next) => Next = next ?? throw new ArgumentNullException(nameof(next));

    public abstract Task HandleAsync(HttpListenerContext context);

    protected Task HandleNextAsync(HttpListenerContext context)
    {
        if (Next != null) return Next.HandleAsync(context);
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        context.Response.Close();
        return Task.CompletedTask;
    }
}
