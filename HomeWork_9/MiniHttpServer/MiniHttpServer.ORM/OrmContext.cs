using Npgsql;

namespace MiniHttpServer.ORM;

public sealed class OrmContext : IOrmContext
{
    private readonly string _connectionString;

    public OrmContext(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<T>> GetAllAsync<T>(
        string tableName,
        CancellationToken cancellationToken = default)
        where T : class, new()
    {
        var command = SqlCommandBuilder.BuildGetAll(tableName);
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = CreateCommand(command, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        return EntityMapper.MapAll<T>(new NpgsqlRecordReader(reader));
    }

    public async Task<T?> GetByIdAsync<T>(
        string tableName,
        int id,
        CancellationToken cancellationToken = default)
        where T : class, new()
    {
        var command = SqlCommandBuilder.BuildGetById(tableName, id);
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = CreateCommand(command, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        return EntityMapper.MapSingle<T>(new NpgsqlRecordReader(reader));
    }

    public async Task<int> InsertAsync<T>(
        string tableName,
        T entity,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var command = SqlCommandBuilder.BuildInsert(tableName, entity);
        return await ExecuteNonQueryAsync(command, cancellationToken);
    }

    public async Task<int> UpdateAsync<T>(
        string tableName,
        int id,
        T entity,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var command = SqlCommandBuilder.BuildUpdate(tableName, id, entity);
        return await ExecuteNonQueryAsync(command, cancellationToken);
    }

    public async Task<int> DeleteAsync(
        string tableName,
        int id,
        CancellationToken cancellationToken = default)
    {
        var command = SqlCommandBuilder.BuildDelete(tableName, id);
        return await ExecuteNonQueryAsync(command, cancellationToken);
    }

    private async Task<int> ExecuteNonQueryAsync(CommandData command, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = CreateCommand(command, connection);
        return await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static NpgsqlCommand CreateCommand(CommandData command, NpgsqlConnection connection)
    {
        var cmd = new NpgsqlCommand(command.Text, connection);
        foreach (var parameter in command.Parameters)
        {
            cmd.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);
        }
        return cmd;
    }

    private sealed class NpgsqlRecordReader : IRecordReader
    {
        private readonly NpgsqlDataReader _reader;

        public NpgsqlRecordReader(NpgsqlDataReader reader) => _reader = reader;

        public int FieldCount => _reader.FieldCount;
        public bool Read() => _reader.Read();
        public string GetName(int index) => _reader.GetName(index);
        public bool IsDBNull(int index) => _reader.IsDBNull(index);
        public object GetValue(int index) => _reader.GetValue(index);
    }
}
