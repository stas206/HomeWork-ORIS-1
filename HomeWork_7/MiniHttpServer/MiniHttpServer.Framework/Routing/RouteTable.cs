using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using MiniHttpServer.Framework.Attributes;

namespace MiniHttpServer.Framework.Routing;

public sealed class RouteTable
{
    private readonly Dictionary<string, ControllerDescriptor> _controllers
        = new(StringComparer.OrdinalIgnoreCase);

    public RouteTable(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        Discover(assembly);
    }

    public IReadOnlyCollection<ControllerDescriptor> Controllers => _controllers.Values;

    private void Discover(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract) continue;
            if (type.GetCustomAttribute<ControllerAttribute>() is null) continue;
            if (type.ContainsGenericParameters) continue;

            var methods = new List<MethodDescriptor>();
            foreach (var method in type.GetMethods(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var getAttr = method.GetCustomAttribute<GetAttribute>();
                var postAttr = method.GetCustomAttribute<PostAttribute>();
                if (getAttr is null && postAttr is null) continue;

                methods.Add(new MethodDescriptor(method, getAttr is not null, postAttr is not null));
            }

            if (methods.Count == 0) continue;

            var routeName = ControllerDescriptor.GetRouteName(type.Name);
            _controllers[routeName] = new ControllerDescriptor(type, routeName, methods);
        }
    }

    public bool TryResolve(
        string controllerName,
        string methodName,
        string httpMethod,
        [NotNullWhen(true)] out ControllerDescriptor? controller,
        [NotNullWhen(true)] out MethodDescriptor? method)
    {
        controller = null;
        method = null;

        if (string.IsNullOrEmpty(controllerName) || string.IsNullOrEmpty(methodName))
            return false;

        if (!_controllers.TryGetValue(controllerName, out controller))
            return false;

        var isGet = httpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase);
        var isPost = httpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase);

        foreach (var candidate in controller.Methods)
        {
            if (!string.Equals(candidate.Name, methodName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (isGet && candidate.AllowGet) { method = candidate; return true; }
            if (isPost && candidate.AllowPost) { method = candidate; return true; }
        }

        return false;
    }
}
