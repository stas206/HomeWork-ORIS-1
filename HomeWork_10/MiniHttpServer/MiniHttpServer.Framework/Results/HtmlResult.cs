using System.Net;
using System.Text;

namespace MiniHttpServer.Framework.Results;

public sealed class HtmlResult : IHttpResult
{
    private const string ContentType = "text/html; charset=utf-8";

    private readonly string _html;
    private readonly HttpStatusCode _statusCode;

    public HtmlResult(string html, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _html = html ?? throw new ArgumentNullException(nameof(html));
        _statusCode = statusCode;
    }

    public ResponseContent BuildResponseContent()
        => new(_statusCode, ContentType, Encoding.UTF8.GetBytes(_html));

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
}
