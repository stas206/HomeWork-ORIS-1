using System.Net;
using System.Text;
using CustomHttpServer.Framework.Results;

namespace CustomHttpServer.Framework.Results;

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

    public async Task ExecuteAsync(
        HttpListenerResponse response,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        var buffer = Encoding.UTF8.GetBytes(_html);

        response.StatusCode = (int)_statusCode;
        response.ContentType = ContentType;
        response.ContentLength64 = buffer.Length;
        response.Headers["Server"] = "CustomHttpServer";

        if (buffer.Length > 0)
        {
            await response.OutputStream.WriteAsync(buffer, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}