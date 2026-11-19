using System.Net;
using System.Net.Mail;
using System.Text;
using CustomHttpServer.Framework.Configuration;

namespace CustomHttpServer.Services;

public sealed class EmailService : IEmailService
{
    public const string FileMode = "File";
    public const string SmtpMode = "Smtp";

    private readonly EmailSettings _settings;
    private readonly string _outputFolder;

    public EmailService(EmailSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _outputFolder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            string.IsNullOrWhiteSpace(_settings.OutputFolder) ? "emails" : _settings.OutputFolder));

        if (IsFileMode)
            Directory.CreateDirectory(_outputFolder);
    }

    private bool IsFileMode =>
        !string.Equals(_settings.Mode, SmtpMode, StringComparison.OrdinalIgnoreCase);

    public Task<EmailResult> SendAsync(string to, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(to))
            return Task.FromResult(EmailResult.Fail("Не указан получатель письма."));
        if (string.IsNullOrWhiteSpace(subject))
            return Task.FromResult(EmailResult.Fail("Не указана тема письма."));

        body ??= string.Empty;

        return IsFileMode
            ? SaveToFileAsync(to, subject, body)
            : SendViaSmtpAsync(to, subject, body);
    }

    private async Task<EmailResult> SendViaSmtpAsync(string to, string subject, string body)
    {
        try
        {
            using var message = new MailMessage(_settings.From, to, subject, body)
            {
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8,
                IsBodyHtml = false
            };

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrEmpty(_settings.UserName))
                client.Credentials = new NetworkCredential(_settings.UserName, _settings.Password);

            await client.SendMailAsync(message);
            Console.WriteLine($"[EmailService] Письмо отправлено через SMTP на {to}.");
            return EmailResult.Ok($"Письмо отправлено через SMTP ({_settings.Host}:{_settings.Port}).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService] Ошибка SMTP: {ex.Message}");
            return EmailResult.Fail(ex.Message);
        }
    }

    private Task<EmailResult> SaveToFileAsync(string to, string subject, string body)
    {
        try
        {
            var fileName = $"email_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.txt";
            var path = Path.Combine(_outputFolder, fileName);

            var content = new StringBuilder()
                .AppendLine($"Date:    {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                .AppendLine($"From:    {_settings.From}")
                .AppendLine($"To:      {to}")
                .AppendLine($"Subject: {subject}")
                .AppendLine(new string('-', 60))
                .AppendLine(body)
                .ToString();

            File.WriteAllText(path, content, Encoding.UTF8);
            Console.WriteLine($"[EmailService] Письмо сохранено в файл: {path}");
            return Task.FromResult(EmailResult.Ok($"Письмо сохранено в файл: {Path.GetFileName(path)}"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService] Ошибка сохранения письма: {ex.Message}");
            return Task.FromResult(EmailResult.Fail(ex.Message));
        }
    }
}
