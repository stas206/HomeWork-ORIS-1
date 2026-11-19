namespace MiniHttpServer.Framework.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AuthorizeAttribute : Attribute
{
}
