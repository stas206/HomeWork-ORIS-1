using System.Net;
using MiniHttpServer.Framework.Content;

namespace MiniHttpServer.Framework.Handlers;

public sealed class StaticFilesHandler : RequestHandler
{
    private readonly string _staticRoot;
    private readonly HashSet<string> _allowedSites;
    private readonly string _basePath;
    private readonly MimeTypeProvider _mimeTypes = new();

    public StaticFilesHandler(string staticRoot, IEnumerable<string> allowedSites, string? basePath = null)
    {
        _staticRoot = Path.GetFullPath(staticRoot);
        _allowedSites = new HashSet<string>(allowedSites ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        _basePath = string.IsNullOrWhiteSpace(basePath) || basePath == "/" ? "" : "/" + basePath.Trim('/');
    }

    public override async Task HandleAsync(HttpListenerContext context)
    {
        var request = context.Request;
        if (!request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
            !request.HttpMethod.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
        {
            await HandleNextAsync(context);
            return;
        }

        try
        {
            var path = request.Url?.AbsolutePath ?? "/";
            if (_basePath.Length > 0)
            {
                if (!path.StartsWith(_basePath + "/", StringComparison.OrdinalIgnoreCase) &&
                    !path.Equals(_basePath, StringComparison.OrdinalIgnoreCase))
                {
                    await HandleNextAsync(context);
                    return;
                }
                path = path[_basePath.Length..];
            }

            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.UnescapeDataString).ToArray();
            if (segments.Length == 0)
            {
                await HandleNextAsync(context);
                return;
            }

            var site = segments[0];
            if (_allowedSites.Count > 0 && !_allowedSites.Contains(site))
            {
                await HandleNextAsync(context);
                return;
            }

            var siteRoot = Path.GetFullPath(Path.Combine(_staticRoot, site));
            if (!IsInside(_staticRoot, siteRoot) || !Directory.Exists(siteRoot))
            {
                await HandleNextAsync(context);
                return;
            }

            var relativeFile = segments.Length > 1 ? Path.Combine(segments.Skip(1).ToArray()) : "index.html";
            var filePath = Path.GetFullPath(Path.Combine(siteRoot, relativeFile));
            if (!IsInside(siteRoot, filePath))
            {
                await HandleNextAsync(context);
                return;
            }

            if (Directory.Exists(filePath)) filePath = Path.Combine(filePath, "index.html");
            if (!File.Exists(filePath))
            {
                await HandleNextAsync(context);
                return;
            }

            var info = new FileInfo(filePath);
            var response = context.Response;
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = _mimeTypes.GetMimeType(filePath);
            response.ContentLength64 = info.Length;
            response.Headers["Server"] = "MiniHttpServer";

            if (request.HttpMethod.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
            {
                response.Close();
                return;
            }

            await using var input = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                81920, useAsync: true);
            await input.CopyToAsync(response.OutputStream);
            response.Close();
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException
                                   or UriFormatException or NotSupportedException)
        {
            await HandleNextAsync(context);
        }
    }

    private static bool IsInside(string root, string candidate)
    {
        var rootWithSeparator = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(candidate).StartsWith(rootWithSeparator,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
}
