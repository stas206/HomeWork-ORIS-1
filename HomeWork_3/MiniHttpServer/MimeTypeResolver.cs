namespace MyHttpServer;

public class MimeTypeResolver
{
    private readonly Dictionary<string, string> _mimeTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { ".html", "text/html; charset=utf-8" },
            { ".htm", "text/html; charset=utf-8" },
            { ".css", "text/css; charset=utf-8" },
            { ".js", "text/javascript; charset=utf-8" },
            { ".json", "application/json" },
            { ".txt", "text/plain; charset=utf-8" },

            { ".png", "image/png" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".gif", "image/gif" },
            { ".webp", "image/webp" },
            { ".svg", "image/svg+xml" },
            { ".ico", "image/x-icon" },

            { ".woff", "font/woff" },
            { ".woff2", "font/woff2" },
            { ".ttf", "font/ttf" },
            { ".otf", "font/otf" },

            { ".pdf", "application/pdf" },
            { ".xml", "application/xml" }
        };

    public string GetContentType(string filePath)
    {
        string extension = Path.GetExtension(filePath);

        if (_mimeTypes.TryGetValue(extension, out string? contentType))
        {
            return contentType;
        }

        return "application/octet-stream";
    }
}