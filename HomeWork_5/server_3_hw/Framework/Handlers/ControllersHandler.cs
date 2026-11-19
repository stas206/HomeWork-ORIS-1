using System.Collections.Specialized;
using System.Net;
using System.Reflection;
using System.Text;
using CustomHttpServer.Framework.Routing;

namespace CustomHttpServer.Framework.Handlers;

public sealed class ControllersHandler : RequestHandler
{
    private readonly RouteTable _routes;
    private readonly IControllerFactory _factory;
    private readonly string _basePath;

    public ControllersHandler(Assembly assembly, IControllerFactory factory, string? basePath = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(factory);
        _routes = new RouteTable(assembly);
        _factory = factory;
        _basePath = string.IsNullOrWhiteSpace(basePath) || basePath == "/"
            ? string.Empty
            : "/" + basePath!.Trim('/');

        if (_routes.Controllers.Count > 0)
        {
            var list = string.Join(", ", _routes.Controllers
                .OrderBy(c => c.Name)
                .Select(c => $"{c.Name} ({c.Methods.Count})"));
            Console.WriteLine($"[ControllersHandler] Обнаружено контроллеров: {_routes.Controllers.Count}. {list}");
        }
    }

    public override async Task HandleAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var httpMethod = request.HttpMethod;

        // Маршрутизация работает только для GET и POST.
        if (!httpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
            !httpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            await HandleNextAsync(context);
            return;
        }

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
            .Select(Uri.UnescapeDataString)
            .ToArray();

        // Для маршрута /{controller}/{method} нужны минимум два сегмента.
        if (segments.Length < 2)
        {
            await HandleNextAsync(context);
            return;
        }

        var controllerName = segments[0];
        var methodName = segments[1];

        if (!_routes.TryResolve(controllerName, methodName, httpMethod, out var controller, out var method))
        {
            await HandleNextAsync(context);
            return;
        }

        try
        {
            await InvokeAsync(context, controller, method);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ControllersHandler] Ошибка вызова {controller.Name}/{method.Name}: {ex}");
            await WriteStatusAsync(context.Response, HttpStatusCode.InternalServerError,
                "Внутренняя ошибка сервера при выполнении метода контроллера.");
        }
    }

    private async Task InvokeAsync(HttpListenerContext context, ControllerDescriptor controller, MethodDescriptor method)
    {
        var values = await CollectValuesAsync(context, method);

        var args = ModelBinder.Bind(method.MethodInfo, values, out var errors);
        if (errors.Count > 0)
        {
            var message = "Некорректные параметры запроса:\n - " + string.Join("\n - ", errors);
            await WriteStatusAsync(context.Response, HttpStatusCode.BadRequest, message);
            return;
        }

        var instance = _factory.Create(controller.ControllerType);
        var result = method.MethodInfo.Invoke(instance, args);

        // Поддержка асинхронных методов: Task и Task<T>.
        if (result is Task task)
        {
            await task;
            result = ExtractTaskResult(task);
        }

        await WriteResultAsync(context.Response, result);
    }

    private static async Task<NameValueCollection> CollectValuesAsync(
        HttpListenerContext context, MethodDescriptor method)
    {
        var values = new NameValueCollection();

        foreach (string key in context.Request.QueryString)
            values[key] = context.Request.QueryString[key];

        if (method.AllowPost && context.Request.HasEntityBody)
        {
            var encoding = context.Request.ContentEncoding ?? Encoding.UTF8;
            using var reader = new StreamReader(context.Request.InputStream, encoding);
            var body = await reader.ReadToEndAsync();
            var form = ModelBinder.ParseForm(body);
            foreach (string key in form)
                values[key] = form[key];
        }

        return values;
    }

    private static object? ExtractTaskResult(Task task)
    {
        var taskType = task.GetType();
        if (!taskType.IsGenericType) return null;

        var resultProperty = taskType.GetProperty("Result");
        return resultProperty?.GetValue(task);
    }

    private static async Task WriteResultAsync(HttpListenerResponse response, object? result)
    {
        byte[] body;
        string contentType;

        switch (result)
        {
            case byte[] bytes:
                body = bytes;
                contentType = "application/octet-stream";
                break;
            case string text:
                body = Encoding.UTF8.GetBytes(text);
                contentType = "text/html; charset=utf-8";
                break;
            case null:
                body = Array.Empty<byte>();
                contentType = "text/plain; charset=utf-8";
                break;
            default:
                body = Encoding.UTF8.GetBytes(result?.ToString() ?? string.Empty);
                contentType = "text/plain; charset=utf-8";
                break;
        }

        response.StatusCode = (int)HttpStatusCode.OK;
        response.ContentType = contentType;
        response.ContentLength64 = body.Length;
        response.Headers["Server"] = "CustomHttpServer";

        if (body.Length > 0)
            await response.OutputStream.WriteAsync(body);

        response.Close();
    }

    private static async Task WriteStatusAsync(HttpListenerResponse response, HttpStatusCode status, string message)
    {
        var html = "<!DOCTYPE html><html lang=\"ru\"><head><meta charset=\"utf-8\">" +
                   $"<title>{(int)status}</title></head><body>" +
                   $"<h1>{(int)status} {status}</h1><pre>{System.Net.WebUtility.HtmlEncode(message)}</pre></body></html>";
        var bytes = Encoding.UTF8.GetBytes(html);

        response.StatusCode = (int)status;
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        response.Headers["Server"] = "CustomHttpServer";
        await response.OutputStream.WriteAsync(bytes);
        response.Close();
    }
}
