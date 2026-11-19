using System.Reflection;

namespace MiniHttpServer.Framework.Routing;

public sealed class MethodDescriptor
{
    public MethodInfo MethodInfo { get; }
    public string Name { get; }
    public bool AllowGet { get; }
    public bool AllowPost { get; }

    public MethodDescriptor(MethodInfo method, bool allowGet, bool allowPost)
    {
        MethodInfo = method ?? throw new ArgumentNullException(nameof(method));
        Name = method.Name;
        AllowGet = allowGet;
        AllowPost = allowPost;
    }
}

public sealed class ControllerDescriptor
{
    private const string Suffix = "Controller";

    public Type ControllerType { get; }
    public string Name { get; }
    public IReadOnlyList<MethodDescriptor> Methods { get; }

    public ControllerDescriptor(Type controllerType, string name, IReadOnlyList<MethodDescriptor> methods)
    {
        ControllerType = controllerType ?? throw new ArgumentNullException(nameof(controllerType));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Methods = methods ?? throw new ArgumentNullException(nameof(methods));
    }

    public static string GetRouteName(string typeName)
    {
        var name = typeName.EndsWith(Suffix, StringComparison.Ordinal)
            ? typeName[..^Suffix.Length]
            : typeName;
        return name.ToLowerInvariant();
    }
}
