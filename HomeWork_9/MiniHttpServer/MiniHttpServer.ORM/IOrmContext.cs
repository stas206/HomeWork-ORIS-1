namespace MiniHttpServer.ORM;

public interface IOrmContext
{
    Task<IReadOnlyList<T>> GetAllAsync<T>(
        string tableName,
        CancellationToken cancellationToken = default)
        where T : class, new();

    Task<T?> GetByIdAsync<T>(
        string tableName,
        int id,
        CancellationToken cancellationToken = default)
        where T : class, new();

    Task<int> InsertAsync<T>(
        string tableName,
        T entity,
        CancellationToken cancellationToken = default)
        where T : class;

    Task<int> UpdateAsync<T>(
        string tableName,
        int id,
        T entity,
        CancellationToken cancellationToken = default)
        where T : class;

    Task<int> DeleteAsync(
        string tableName,
        int id,
        CancellationToken cancellationToken = default);
}
