using System.Reflection;

namespace MiniHttpServer.ORM;

public static class EntityMapper
{
    public static IReadOnlyList<T> MapAll<T>(IRecordReader reader) where T : class, new()
    {
        var columns = ReadColumns(reader);
        var result = new List<T>();
        while (reader.Read())
        {
            result.Add(MapRow<T>(reader, columns));
        }
        return result;
    }

    public static T? MapSingle<T>(IRecordReader reader) where T : class, new()
    {
        var columns = ReadColumns(reader);
        return reader.Read() ? MapRow<T>(reader, columns) : null;
    }

    public static T MapRow<T>(IRecordReader reader) where T : class, new()
    {
        return MapRow<T>(reader, ReadColumns(reader));
    }

    private static Dictionary<string, int> ReadColumns(IRecordReader reader)
    {
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < reader.FieldCount; i++)
        {
            columns[reader.GetName(i)] = i;
        }
        return columns;
    }

    private static T MapRow<T>(IRecordReader reader, IReadOnlyDictionary<string, int> columns)
        where T : class, new()
    {
        var entity = new T();
        foreach (var property in GetWritableProperties<T>())
        {
            if (!columns.TryGetValue(property.Name, out var ordinal))
            {
                continue;
            }

            if (reader.IsDBNull(ordinal))
            {
                if (!property.PropertyType.IsValueType ||
                    Nullable.GetUnderlyingType(property.PropertyType) is not null)
                {
                    property.SetValue(entity, null);
                }
                continue;
            }

            property.SetValue(entity, SqlValueConverter.Convert(reader.GetValue(ordinal), property.PropertyType));
        }
        return entity;
    }

    private static IReadOnlyList<PropertyInfo> GetWritableProperties<T>()
    {
        return typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.SetMethod!.IsPublic)
            .ToArray();
    }
}
