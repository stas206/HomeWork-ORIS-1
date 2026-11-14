namespace CustomHttpServer.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class HttpControllerAttribute : Attribute
{
    public string Name { get; }

    public HttpControllerAttribute(string name)
    {
        Name = name;
    }
}