using System.Net;
using System.Text;
using CustomHttpServer.Framework.Results;

namespace CustomHttpServer.Framework.Results;

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

    public async Task ExecuteAsync(
        HttpListenerResponse response,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        var buffer = Encoding.UTF8.GetBytes(_text);

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