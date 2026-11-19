namespace MiniHttpServer.Framework.Authentication;

public interface IUserSessionService
{
    string CreateSession(int userId);

    int? GetUserId(string? sessionId);

    void RemoveSession(string? sessionId);
}
