using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Authentication;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;

namespace MiniHttpServer.Framework.UnitTests.Fixtures;

[Controller]
public sealed class ProtectedController : BaseController
{
    [Get]
    [Authorize]
    public IHttpResult Cabinet() => new TextResult("cabinet");

    [Get]
    public IHttpResult Open() => new TextResult("open");
}

[Controller]
[Authorize]
public sealed class RestrictedController : BaseController
{
    [Get]
    public IHttpResult Secret() => new TextResult("secret");
}

public sealed class FakeSessionService : IUserSessionService
{
    public string? ValidSessionId { get; set; } = "session-ok";
    public int UserId { get; set; } = 1;

    public string CreateSession(int userId) =>
        ValidSessionId ?? Guid.NewGuid().ToString("N");

    public int? GetUserId(string? sessionId) =>
        sessionId == ValidSessionId ? UserId : null;

    public void RemoveSession(string? sessionId) { }
}
