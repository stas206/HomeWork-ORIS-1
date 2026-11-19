namespace MiniHttpServer.Framework.Attributes;

/// <summary>
/// Помечает метод контроллера как обработчик HTTP-запросов методом GET.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class GetAttribute : Attribute
{
}