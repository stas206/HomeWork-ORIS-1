using System.Net;

namespace MiniHttpServer.Framework.Results;

/// <summary>
/// Контракт результата выполнения метода контроллера.
/// Реализация отвечает за формирование и запись HTTP-ответа
/// в <see cref="HttpListenerResponse"/>.
/// </summary>
public interface IHttpResult
{
    /// <summary>
    /// Формирует и записывает HTTP-ответ в указанный объект ответа.
    /// </summary>
    /// <param name="response">Объект ответа HTTP-сервера.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task ExecuteAsync(
        HttpListenerResponse response,
        CancellationToken cancellationToken = default);
}