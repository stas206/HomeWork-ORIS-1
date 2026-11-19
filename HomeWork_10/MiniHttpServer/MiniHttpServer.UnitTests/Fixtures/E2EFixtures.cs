using MiniHttpServer.HtmlTemplating;
using MiniHttpServer.ORM;
using MiniHttpServer.Services;

namespace MiniHttpServer.UnitTests.Fixtures;

public sealed class FakeEmailService : IEmailService
{
    public Task<EmailResult> SendAsync(string to, string subject, string body)
        => Task.FromResult(EmailResult.Ok("stub"));
}

public sealed class StubOrmContext : IOrmContext
{
    public Task<IReadOnlyList<T>> GetAllAsync<T>(
        string tableName, CancellationToken cancellationToken = default)
        where T : class, new()
        => Task.FromResult<IReadOnlyList<T>>(Array.Empty<T>());

    public Task<T?> GetByIdAsync<T>(
        string tableName, int id, CancellationToken cancellationToken = default)
        where T : class, new()
        => Task.FromResult<T?>(null);

    public Task<int> InsertAsync<T>(
        string tableName, T entity, CancellationToken cancellationToken = default)
        where T : class
        => Task.FromResult(0);

    public Task<int> UpdateAsync<T>(
        string tableName, int id, T entity, CancellationToken cancellationToken = default)
        where T : class
        => Task.FromResult(0);

    public Task<int> DeleteAsync(
        string tableName, int id, CancellationToken cancellationToken = default)
        => Task.FromResult(0);
}

public sealed class StubTemplateRenderer : IHtmlTemplateRenderer
{
    public string RenderFromString(string htmlTemplate, object? dataModel) => htmlTemplate;

    public string RenderFromFile(string filePath, object? dataModel) => "<html><body>stub</body></html>";

    public string RenderToFile(string inputFilePath, string outputFilePath, object? dataModel)
        => RenderFromFile(inputFilePath, dataModel);
}
