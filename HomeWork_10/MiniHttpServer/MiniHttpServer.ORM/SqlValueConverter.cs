using System.Globalization;

namespace MiniHttpServer.ORM;

public static class SqlValueConverter
{
    public static object? Convert(object? value, Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value is null or DBNull)
        {
            return targetType.IsValueType && Nullable.GetUnderlyingType(targetType) is null
                ? Activator.CreateInstance(targetType)
                : null;
        }

        if (underlying.IsInstanceOfType(value))
        {
            return value;
        }

        if (underlying.IsEnum)
        {
            return Enum.ToObject(underlying, value);
        }

        return System.Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
    }
}
