using System.Collections.Specialized;
using System.Reflection;
using System.Globalization;

namespace MiniHttpServer.Framework.Routing;

public static class ModelBinder
{
    public static object?[] Bind(MethodInfo method, NameValueCollection values, out IReadOnlyList<string> errors)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(values);

        var errorList = new List<string>();
        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var raw = values[parameter.Name];

            if (string.IsNullOrEmpty(raw))
            {
                if (parameter.HasDefaultValue)
                    args[i] = parameter.DefaultValue;
                else if (IsNonNullableValueType(parameter.ParameterType))
                {
                    errorList.Add($"Отсутствует обязательный параметр '{parameter.Name}'.");
                    args[i] = GetTypeDefault(parameter.ParameterType);
                }
                else
                    args[i] = null;
                continue;
            }

            if (!TryConvert(raw, parameter.ParameterType, out var converted))
            {
                errorList.Add(
                    $"Не удалось преобразовать параметр '{parameter.Name}' " +
                    $"из значения '{raw}' к типу {parameter.ParameterType.Name}.");
                args[i] = parameter.HasDefaultValue
                    ? parameter.DefaultValue
                    : GetTypeDefault(parameter.ParameterType);
            }
            else
            {
                args[i] = converted;
            }
        }

        errors = errorList;
        return args;
    }

    public static NameValueCollection ParseForm(string body)
    {
        var result = new NameValueCollection();
        if (string.IsNullOrEmpty(body)) return result;

        foreach (var pair in body.Split('&'))
        {
            if (string.IsNullOrEmpty(pair)) continue;

            var eq = pair.IndexOf('=');
            string key;
            string value;
            if (eq < 0)
            {
                key = Decode(pair);
                value = string.Empty;
            }
            else
            {
                key = Decode(pair[..eq]);
                value = Decode(pair[(eq + 1)..]);
            }

            if (!string.IsNullOrEmpty(key))
                result[key] = value;
        }

        return result;
    }

    private static bool TryConvert(string raw, Type targetType, out object? converted)
    {
        var underlying = Nullable.GetUnderlyingType(targetType);
        var type = underlying ?? targetType;
        converted = null;

        if (type == typeof(string)) { converted = raw; return true; }

        if (type == typeof(bool))
        {
            if (bool.TryParse(raw, out var b)) { converted = b; return true; }
            if (raw.Equals("on", StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                raw == "1")
            { converted = true; return true; }
            if (raw == "0" || raw.Equals("off", StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("no", StringComparison.OrdinalIgnoreCase))
            { converted = false; return true; }
            return false;
        }

        try
        {
            converted = Convert.ChangeType(raw, type, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsNonNullableValueType(Type type) =>
        type.IsValueType && Nullable.GetUnderlyingType(type) is null;

    private static object? GetTypeDefault(Type type) =>
        type.IsValueType ? Activator.CreateInstance(type) : null;

    private static string Decode(string segment)
    {
        return Uri.UnescapeDataString(segment.Replace('+', ' '));
    }
}
