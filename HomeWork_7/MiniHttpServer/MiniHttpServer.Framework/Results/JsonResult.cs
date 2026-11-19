using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace MiniHttpServer.Framework.Results;

public sealed class JsonResult : IHttpResult
{
    private const string ContentType = "application/json; charset=utf-8";

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly object _data;
    private readonly HttpStatusCode _statusCode;

    public JsonResult(object data, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _statusCode = statusCode;
    }

    public async Task ExecuteAsync(
        HttpListenerResponse response,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        var buffer = JsonSerializer.SerializeToUtf8Bytes(_data, _serializerOptions);

        response.StatusCode = (int)_statusCode;
        response.ContentType = ContentType;
        response.ContentLength64 = buffer.Length;
        response.Headers["Server"] = "MiniHttpServer";

        if (buffer.Length > 0)
        {
            await response.OutputStream.WriteAsync(buffer, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
