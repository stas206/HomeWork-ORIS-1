namespace CustomHttpServer.Framework.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class GetAttribute : Attribute
{
}