namespace MiniHttpServer.Framework.Routing;

public interface IControllerFactory
{
    object Create(Type controllerType);
}
