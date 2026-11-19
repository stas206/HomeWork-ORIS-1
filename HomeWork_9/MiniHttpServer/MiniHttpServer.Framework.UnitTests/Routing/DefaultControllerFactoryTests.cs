using MiniHttpServer.Framework.UnitTests.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Framework.Routing;

namespace MiniHttpServer.Framework.UnitTests.Routing;

[TestClass]
public class DefaultControllerFactoryTests
{
    [TestMethod]
    public void Create_ParameterlessController_ReturnsInstance()
    {
        var factory = new DefaultControllerFactory();

        var instance = factory.Create(typeof(CatalogController));

        Assert.IsNotNull(instance);
        Assert.IsInstanceOfType(instance, typeof(CatalogController));
    }

    [TestMethod]
    public void Create_RegisteredService_InjectedIntoController()
    {
        var factory = new DefaultControllerFactory();
        var counter = new CounterService { Value = 42 };
        factory.Register<CounterService>(() => counter);

        var instance = factory.Create(typeof(StatsController));

        var stats = instance as StatsController;
        Assert.IsNotNull(stats);
        Assert.AreSame(counter, stats!.Counter);
        Assert.AreEqual(42, stats.Counter.Value);
    }

    [TestMethod]
    public void Create_ParameterWithDefaultValue_UsesDefaultWhenServiceMissing()
    {
        var factory = new DefaultControllerFactory();

        var instance = factory.Create(typeof(OptionalController));

        var optional = instance as OptionalController;
        Assert.IsNotNull(optional);
        Assert.AreEqual("fallback", optional!.Name);
    }

    [TestMethod]
    public void Register_NullFactory_ThrowsArgumentNullException()
    {
        var factory = new DefaultControllerFactory();

        Assert.ThrowsException<ArgumentNullException>(
            () => factory.Register<CounterService>(null!));
    }
}
