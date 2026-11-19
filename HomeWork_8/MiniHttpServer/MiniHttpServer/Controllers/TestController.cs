using System.Net;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;

namespace MiniHttpServer.Controllers;

[Controller]
public sealed class TestController : BaseController
{
    [Get]
    public IHttpResult Index()
    {
        return Html("<!DOCTYPE html><html lang=\"ru\"><head><meta charset=\"utf-8\">" +
                    "<title>Index</title></head><body>" +
                    "<h1>Hello from controller!</h1></body></html>");
    }

    [Get]
    public IHttpResult Hello()
    {
        return Html("<h1>Hello World!</h1>");
    }

    [Get]
    public IHttpResult Info()
    {
        return Json(new
        {
            Id = 1,
            Message = "Hello World!",
            Success = true
        });
    }

    [Post]
    public IHttpResult Submit()
    {
        return Html("<h1>Submit</h1><p>POST request processed</p>");
    }

    [Get]
    public IHttpResult Details(int id, string name)
    {
        return Html($"<h1>Details</h1>" +
                    $"<p>ID: {WebUtility.HtmlEncode(id.ToString())}</p>" +
                    $"<p>Name: {WebUtility.HtmlEncode(name ?? string.Empty)}</p>");
    }

    [Post]
    public IHttpResult Echo(string demoUser, string message)
    {
        return Html($"<h1>Echo</h1>" +
                    $"<p>User: {WebUtility.HtmlEncode(demoUser ?? string.Empty)}</p>" +
                    $"<p>Message: {WebUtility.HtmlEncode(message ?? string.Empty)}</p>");
    }

    [Get]
    public IHttpResult RequestInfo()
    {
        var request = Context.Request;

        return Json(new
        {
            method = request.HttpMethod,
            path = request.Url?.AbsolutePath ?? "/",
            userAgent = request.Headers["User-Agent"] ?? string.Empty
        });
    }
}
