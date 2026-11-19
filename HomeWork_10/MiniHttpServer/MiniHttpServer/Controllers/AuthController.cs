using System.Net;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.Services;

namespace MiniHttpServer.Controllers;

[Controller]
public sealed class AuthController : BaseController
{
    private readonly IEmailService _emailService;

    public AuthController(IEmailService emailService)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
    }

    [Post]
    public async Task<IHttpResult> SendEmail(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        {
            return Json(new
            {
                success = false,
                message = "Не заполнены обязательные поля формы."
            }, HttpStatusCode.BadRequest);
        }

        var subject = $"Учебное письмо - пользователь {login}";
        var body =
            "Дин-дон, вам письмо.\n" +
            $"Пользователь: {login}\n" +
            $"Время: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";

        try
        {
            var result = await _emailService.SendAsync("iuoris@mail.ru", subject, body)
                .ConfigureAwait(false);

            if (result.Success)
            {
                return Json(new
                {
                    success = true,
                    message = "Сообщение отправлено"
                });
            }

            Console.WriteLine($"[AuthController] Не удалось отправить письмо: {result.Message}");
            return Json(new
            {
                success = false,
                message = "Не удалось отправить сообщение"
            }, HttpStatusCode.InternalServerError);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthController] Ошибка отправки письма: {ex}");
            return Json(new
            {
                success = false,
                message = "Не удалось отправить сообщение"
            }, HttpStatusCode.InternalServerError);
        }
    }

    [Post]
    public IHttpResult Login(string login, string password)
    {
        const string testLogin = "student";
        const string testPassword = "test123";

        if (login != testLogin || password != testPassword)
        {
            return Json(new
            {
                Success = false,
                Message = "Неверный логин или пароль"
            }, HttpStatusCode.Unauthorized);
        }

        int userId = 1;
        string sessionId = SessionService.CreateSession(userId);

        var cookie = new Cookie("sessionId", sessionId, "/")
        {
            HttpOnly = true,
            Secure = Context.Request.IsSecureConnection,
            Expires = DateTime.UtcNow.AddMinutes(30)
        };

        Context.Response.SetCookie(cookie);

        return Json(new
        {
            Success = true,
            Message = "Вход выполнен"
        });
    }

    [Post]
    public IHttpResult Logout()
    {
        string? sessionId = Context.Request.Cookies["sessionId"]?.Value;

        SessionService.RemoveSession(sessionId);

        var cookie = new Cookie("sessionId", string.Empty, "/")
        {
            HttpOnly = true,
            Secure = Context.Request.IsSecureConnection,
            Expires = DateTime.UtcNow.AddDays(-1)
        };

        Context.Response.SetCookie(cookie);

        return Json(new
        {
            Success = true,
            Message = "Выход выполнен"
        });
    }
}
