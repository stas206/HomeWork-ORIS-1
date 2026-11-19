using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using MiniHttpServer.Framework.Routing;

namespace MiniHttpServer.Framework.UnitTests.Routing;

[TestClass]
public class RouteTableTests
{
    private static RouteTable CreateSut() => new(Assembly.GetExecutingAssembly());

    [TestMethod]
    public void Discover_RegistersControllerAttributeClasses()
    {
        var routes = CreateSut();

        var names = routes.Controllers.Select(c => c.Name).ToList();

        CollectionAssert.Contains(names, "account");
        CollectionAssert.Contains(names, "catalog");
        CollectionAssert.Contains(names, "stats");
    }

    [TestMethod]
    public void Discover_IgnoresClassesWithoutControllerAttribute()
    {
        var routes = CreateSut();

        var names = routes.Controllers.Select(c => c.Name).ToList();

        CollectionAssert.DoesNotContain(names, "plain");
    }

    [TestMethod]
    public void Discover_RegistersOnlyAttributedMethodsAsActions()
    {
        var routes = CreateSut();

        var account = routes.Controllers.Single(c => c.Name == "account");

        var methodNames = account.Methods.Select(m => m.Name).ToList();
        CollectionAssert.Contains(methodNames, "Info");
        CollectionAssert.Contains(methodNames, "Login");
        CollectionAssert.DoesNotContain(methodNames, "InternalHelper");
    }

    [TestMethod]
    public void TryResolve_KnownGetRoute_ReturnsControllerAndMethod()
    {
        var routes = CreateSut();

        var ok = routes.TryResolve("account", "info", "GET", out var controller, out var method);

        Assert.IsTrue(ok);
        Assert.IsNotNull(controller);
        Assert.IsNotNull(method);
        Assert.AreEqual("account", controller!.Name);
        Assert.AreEqual("Info", method!.Name);
        Assert.IsTrue(method.AllowGet);
    }

    [TestMethod]
    public void TryResolve_PostRoute_ResolvedForPostMethod()
    {
        var routes = CreateSut();

        var ok = routes.TryResolve("account", "login", "POST", out _, out var method);

        Assert.IsTrue(ok);
        Assert.IsNotNull(method);
        Assert.IsTrue(method!.AllowPost);
        Assert.IsFalse(method.AllowGet);
    }

    [TestMethod]
    public void TryResolve_PostMethodNotCallableByGet_ReturnsFalse()
    {
        var routes = CreateSut();

        var ok = routes.TryResolve("account", "login", "GET", out _, out _);

        Assert.IsFalse(ok);
    }

    [TestMethod]
    public void TryResolve_UnknownController_ReturnsFalse()
    {
        var routes = CreateSut();

        var ok = routes.TryResolve("missing", "info", "GET", out _, out _);

        Assert.IsFalse(ok);
    }

    [TestMethod]
    public void TryResolve_UnknownMethod_ReturnsFalse()
    {
        var routes = CreateSut();

        var ok = routes.TryResolve("account", "missing", "GET", out _, out _);

        Assert.IsFalse(ok);
    }

    [TestMethod]
    public void TryResolve_RouteNamesAreCaseInsensitive()
    {
        var routes = CreateSut();

        var ok = routes.TryResolve("ACCOUNT", "INFO", "GET", out _, out _);

        Assert.IsTrue(ok);
    }

    [TestMethod]
    public void Constructor_NullAssembly_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new RouteTable(null!));
    }
}
