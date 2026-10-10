using CustomHttpServer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CustomHttpServer.Framework;

public class RouteInfo
{
    public string ControllerName { get; set; }
    public string MethodName { get; set; }
    public string Path { get; set; }
    public MethodInfo Method { get; set; }
    public object ControllerInstance { get; set; }
}

public class Router
{
    private readonly List<RouteInfo> _routes = new();

    public void RegisterControllers(string htmlFolder)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    var controllerAttr = type.GetCustomAttribute<HttpControllerAttribute>();
                    if (controllerAttr == null) continue;

                    var controllerInstance = Activator.CreateInstance(type, htmlFolder);
                    string controllerName = controllerAttr.Name;

                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

                    foreach (var method in methods)
                    {
                        var getAttr = method.GetCustomAttribute<GetAttribute>();
                        if (getAttr == null) continue;

                        _routes.Add(new RouteInfo
                        {
                            ControllerName = controllerName,
                            MethodName = method.Name,
                            Path = getAttr.Path,
                            Method = method,
                            ControllerInstance = controllerInstance
                        });

                        Console.WriteLine($"📍 Зарегистрирован маршрут: GET /{controllerName}/{getAttr.Path}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Игнорируем ошибки загрузки сборок
            }
        }
    }

    public bool HasController(string controllerName)
    {
        return _routes.Any(r => 
            r.ControllerName.Equals(controllerName, StringComparison.OrdinalIgnoreCase));
    }

    public RouteInfo FindRoute(string controllerName, string path)
    {
        return _routes.FirstOrDefault(r =>
            r.ControllerName.Equals(controllerName, StringComparison.OrdinalIgnoreCase) &&
            r.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
    }

    public string ExecuteRoute(RouteInfo route, Dictionary<string, string> queryParams)
    {
        var method = route.Method;
        var parameters = method.GetParameters();
        var arguments = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            string paramName = param.Name.ToLower();

            if (queryParams.TryGetValue(paramName, out string value))
            {
                arguments[i] = Convert.ChangeType(value, param.ParameterType);
            }
            else
            {
                arguments[i] = param.HasDefaultValue ? param.DefaultValue : null;
            }
        }

        var result = method.Invoke(route.ControllerInstance, arguments);
        return result?.ToString() ?? "";
    }
}