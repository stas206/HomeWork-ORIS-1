using CustomHttpServer.Framework.Attributes;
using CustomHttpServer.Services;

namespace CustomHttpServer.Controllers;

[Controller]
public sealed class AuthController
{
    private readonly IEmailService _emailService;

    public AuthController(IEmailService emailService)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
    }
    
    [Post]
    public async Task<string> SendEmail(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login))
            return "Ошибка: поле 'login' обязательно для заполнения.";
        if (string.IsNullOrWhiteSpace(password))
            return "Ошибка: поле 'password' обязательно для заполнения.";

        var subject = $"Учебное письмо - пользователь {login}";
        var body =
            "Дин-дон, вам письмо.\n" +
            $"Пользователь: {login}\n" +
            $"Пароль: {password}\n\n" +
            $"Время: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";

        var result = await _emailService.SendAsync("iuoris@mail.ru", subject, body);

        return result.Success
            ? $"OK: {result.Message}"
            : $"FAIL: не удалось отправить письмо - {result.Message}";
    }
}