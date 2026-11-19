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

    public ResponseContent BuildResponseContent()
        => new(_statusCode, ContentType, JsonSerializer.SerializeToUtf8Bytes(_data, _serializerOptions));

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
