using System.Net;

namespace CustomHttpServer.Framework.Results;

public interface IHttpResult
{
    Task ExecuteAsync(
        HttpListenerResponse response,
        CancellationToken cancellationToken = default);
}