using System.Reflection;

namespace MiniHttpServer.Framework.Routing;

public sealed class DefaultControllerFactory : IControllerFactory
{
    private readonly Dictionary<Type, Func<object>> _factories = new();

    public void Register<TService>(Func<TService> factory) where TService : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factories[typeof(TService)] = () => factory()!;
    }

    public object Create(Type controllerType)
    {
        ArgumentNullException.ThrowIfNull(controllerType);

        var constructors = controllerType
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .ToArray();

        if (constructors.Length == 0)
            return Activator.CreateInstance(controllerType)!;

        var chosen = constructors.FirstOrDefault(CanSatisfy) ?? constructors[0];
        var args = chosen.GetParameters()
            .Select(ResolveArgument)
            .ToArray();

        return Activator.CreateInstance(controllerType, args)!;
    }

    private bool CanSatisfy(ConstructorInfo ctor)
    {
        foreach (var parameter in ctor.GetParameters())
        {
            if (_factories.ContainsKey(parameter.ParameterType)) continue;
            if (parameter.HasDefaultValue) continue;
            if (!IsNonNullableValueType(parameter.ParameterType)) continue; 
            return false;
        }
        return true;
    }

    private object? ResolveArgument(ParameterInfo parameter)
    {
        if (_factories.TryGetValue(parameter.ParameterType, out var factory))
            return factory();
        if (parameter.HasDefaultValue)
            return parameter.DefaultValue;
        return GetTypeDefault(parameter.ParameterType);
    }

    private static bool IsNonNullableValueType(Type type) =>
        type.IsValueType && Nullable.GetUnderlyingType(type) is null;

    private static object? GetTypeDefault(Type type) =>
        type.IsValueType ? Activator.CreateInstance(type) : null;
}
