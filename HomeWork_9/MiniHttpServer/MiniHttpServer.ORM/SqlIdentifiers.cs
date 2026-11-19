using System.Text.RegularExpressions;

namespace MiniHttpServer.ORM;

public static class SqlIdentifiers
{
    private static readonly Regex IdentifierPattern =
        new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public static string Escape(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier) ||
            !IdentifierPattern.IsMatch(identifier))
        {
            throw new ArgumentException(
                "Недопустимое имя SQL-идентификатора.", nameof(identifier));
        }

        return $"\"{identifier}\"";
    }
}
