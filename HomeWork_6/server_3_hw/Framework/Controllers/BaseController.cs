using System.Net;
using CustomHttpServer.Framework.Results;

namespace CustomHttpServer.Framework.Controllers;

public abstract class BaseController
{
    protected HttpListenerContext Context { get; private set; } = null!;

    internal void Initialize(HttpListenerContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    protected IHttpResult Html(string html, HttpStatusCode statusCode = HttpStatusCode.OK)
        => new HtmlResult(html, statusCode);

    protected IHttpResult Json(object data, HttpStatusCode statusCode = HttpStatusCode.OK)
        => new JsonResult(data, statusCode);

    protected IHttpResult Text(string text, HttpStatusCode statusCode = HttpStatusCode.OK)
        => new TextResult(text, statusCode);
}
