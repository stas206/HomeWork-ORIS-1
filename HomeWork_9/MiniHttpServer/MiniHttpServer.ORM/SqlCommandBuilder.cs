using System.Reflection;

namespace MiniHttpServer.ORM;

public static class SqlCommandBuilder
{
    public static CommandData BuildGetAll(string tableName)
    {
        return new CommandData(
            $"SELECT * FROM {SqlIdentifiers.Escape(tableName)}",
            Array.Empty<ParameterData>());
    }

    public static CommandData BuildGetById(string tableName, int id)
    {
        return new CommandData(
            $"SELECT * FROM {SqlIdentifiers.Escape(tableName)} WHERE \"Id\" = @id",
            new[] { new ParameterData("@id", id) });
    }

    public static CommandData BuildInsert<T>(string tableName, T entity) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        var properties = GetMappableProperties<T>();
        var table = SqlIdentifiers.Escape(tableName);
        var columns = string.Join(", ", properties.Select(p => $"\"{p.Name}\""));
        var values = string.Join(", ", properties.Select(p => "@" + p.Name));
        var text = $"INSERT INTO {table} ({columns}) VALUES ({values})";
        var parameters = properties
            .Select(p => new ParameterData("@" + p.Name, p.GetValue(entity)))
            .ToArray();

        return new CommandData(text, parameters);
    }

    public static CommandData BuildUpdate<T>(string tableName, int id, T entity) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        var properties = GetMappableProperties<T>();
        var table = SqlIdentifiers.Escape(tableName);
        var setClause = string.Join(", ", properties.Select(p => $"\"{p.Name}\" = @{p.Name}"));
        var text = $"UPDATE {table} SET {setClause} WHERE \"Id\" = @id";

        var parameters = new List<ParameterData>(
            properties.Select(p => new ParameterData("@" + p.Name, p.GetValue(entity))))
        {
            new("@id", id)
        };

        return new CommandData(text, parameters);
    }

    public static CommandData BuildDelete(string tableName, int id)
    {
        return new CommandData(
            $"DELETE FROM {SqlIdentifiers.Escape(tableName)} WHERE \"Id\" = @id",
            new[] { new ParameterData("@id", id) });
    }

    private static IReadOnlyList<PropertyInfo> GetMappableProperties<T>()
    {
        return typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite &&
                        p.SetMethod!.IsPublic &&
                        !string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
