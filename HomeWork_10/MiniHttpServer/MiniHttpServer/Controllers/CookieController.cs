using System.Net;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;

namespace MiniHttpServer.Controllers;

[Controller]
public sealed class CookieController : BaseController
{
    [Get]
    public IHttpResult Set()
    {
        var cookie = new Cookie("testCookie", "HelloWorld", "/")
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddMinutes(30)
        };

        Context.Response.SetCookie(cookie);

        return Html("<h1>Cookie установлена</h1>");
    }

    [Get]
    public IHttpResult Get()
    {
        string? value = Context.Request.Cookies["testCookie"]?.Value;

        return Json(new { Value = value });
    }

    [Get]
    public IHttpResult Delete()
    {
        var cookie = new Cookie("testCookie", string.Empty, "/")
        {
            Expires = DateTime.UtcNow.AddDays(-1)
        };

        Context.Response.SetCookie(cookie);

        return Html("<h1>Cookie удалена</h1>");
    }
}
