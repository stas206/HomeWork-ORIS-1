using System.Net;
using MiniHttpServer.Framework.Authentication;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.HtmlTemplating;

namespace MiniHttpServer.Framework.Controllers;

public abstract class BaseController
{
    protected HttpListenerContext Context { get; private set; } = null!;

    protected IHtmlTemplateRenderer TemplateRenderer { get; private set; } = null!;

    protected IUserSessionService SessionService { get; private set; } = null!;

    internal void Initialize(
        HttpListenerContext context,
        IHtmlTemplateRenderer templateRenderer,
        IUserSessionService sessionService)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        TemplateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
        SessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
    }

    protected IHttpResult Html(string html, HttpStatusCode statusCode = HttpStatusCode.OK)
        => new HtmlResult(html, statusCode);

    protected IHttpResult Json(object data, HttpStatusCode statusCode = HttpStatusCode.OK)
        => new JsonResult(data, statusCode);

    protected IHttpResult Text(string text, HttpStatusCode statusCode = HttpStatusCode.OK)
        => new TextResult(text, statusCode);

    protected IHttpResult Page(
        string templatePath,
        object? model,
        HttpStatusCode statusCode = HttpStatusCode.OK)
        => new PageResult(TemplateRenderer, templatePath, model, statusCode);
}
