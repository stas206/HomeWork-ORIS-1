using System.Net;
using System.Text;

namespace MiniHttpServer.Framework.Results;

public sealed class TextResult : IHttpResult
{
    private const string ContentType = "text/plain; charset=utf-8";

    private readonly string _text;
    private readonly HttpStatusCode _statusCode;

    public TextResult(string text, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _statusCode = statusCode;
    }

    public ResponseContent BuildResponseContent()
        => new(_statusCode, ContentType, Encoding.UTF8.GetBytes(_text));

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
