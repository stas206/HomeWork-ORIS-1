using System.Net;
using MiniHttpServer.HtmlTemplating;

namespace MiniHttpServer.Framework.Results;

public sealed class PageResult : IHttpResult
{
    public const string TemplatesFolder = "Templates";

    private readonly IHtmlTemplateRenderer _renderer;
    private readonly string _templatePath;
    private readonly object? _model;
    private readonly HttpStatusCode _statusCode;

    public PageResult(
        IHtmlTemplateRenderer renderer,
        string templatePath,
        object? model,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _templatePath = templatePath ?? throw new ArgumentNullException(nameof(templatePath));
        _model = model;
        _statusCode = statusCode;
    }

    public ResponseContent BuildResponseContent()
    {
        var fullPath = ResolveTemplatePath(_templatePath);
        var html = _renderer.RenderFromFile(fullPath, _model);
        return new HtmlResult(html, _statusCode).BuildResponseContent();
    }

    public async Task ExecuteAsync(
        HttpListenerResponse response,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        var content = BuildResponseContent();

        response.StatusCode = (int)content.StatusCode;
        response.ContentType = content.ContentType;
        response.ContentLength64 = content.Body.Length;
        response.Headers["Server"] = content.Server;

        if (content.Body.Length > 0)
        {
            await response.OutputStream.WriteAsync(content.Body, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static string ResolveTemplatePath(string templatePath)
    {
        if (Path.IsPathRooted(templatePath) && File.Exists(templatePath))
            return templatePath;

        var baseDir = AppContext.BaseDirectory;
        var candidate = Path.GetFullPath(Path.Combine(baseDir, TemplatesFolder, templatePath));
        if (File.Exists(candidate)) return candidate;

        var dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 6 && dir is not null; i++)
        {
            var probe = Path.Combine(dir.FullName, TemplatesFolder, templatePath);
            if (File.Exists(probe)) return Path.GetFullPath(probe);
            dir = dir.Parent;
        }

        return candidate;
    }
}
