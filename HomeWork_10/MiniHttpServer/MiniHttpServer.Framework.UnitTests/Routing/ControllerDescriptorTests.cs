using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Framework.Routing;

namespace MiniHttpServer.Framework.UnitTests.Routing;

[TestClass]
public class ControllerDescriptorTests
{
    [TestMethod]
    public void GetRouteName_WithControllerSuffix_StripsSuffixAndLowercases()
    {
        var route = ControllerDescriptor.GetRouteName("UserController");

        Assert.AreEqual("user", route);
    }

    [TestMethod]
    public void GetRouteName_WithoutSuffix_ReturnsLowercasedName()
    {
        var route = ControllerDescriptor.GetRouteName("Catalog");

        Assert.AreEqual("catalog", route);
    }

    [TestMethod]
    public void GetRouteName_MixedCase_LowercasesAll()
    {
        var route = ControllerDescriptor.GetRouteName("MyApiController");

        Assert.AreEqual("myapi", route);
    }
}
