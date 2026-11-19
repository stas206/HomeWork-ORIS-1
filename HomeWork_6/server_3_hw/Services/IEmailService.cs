using CustomHttpServer.Framework.Configuration;

namespace CustomHttpServer.Services;

public interface IEmailService
{
    Task<EmailResult> SendAsync(string to, string subject, string body);
}

public sealed class EmailResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }

    public static EmailResult Ok(string? message = null) => new() { Success = true, Message = message };
    public static EmailResult Fail(string message) => new() { Success = false, Message = message };
}