using System.Net;

namespace MiniHttpServer.Framework.Results;

public sealed record ResponseContent(
    HttpStatusCode StatusCode,
    string ContentType,
    byte[] Body,
    string Server = "MiniHttpServer");
