using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.UnitTests.Fixtures;

namespace MiniHttpServer.Framework.UnitTests.Attributes;

[TestClass]
public class AuthorizeAttributeTests
{
    [TestMethod]
    public void MethodWithoutAuthorize_IsNotMarkedAuthorized()
    {
        var method = typeof(ProtectedController).GetMethod(nameof(ProtectedController.Open))!;

        Assert.IsFalse(method.IsDefined(typeof(AuthorizeAttribute), true));
    }

    [TestMethod]
    public void MethodWithAuthorize_IsMarkedAuthorized()
    {
        var method = typeof(ProtectedController).GetMethod(nameof(ProtectedController.Cabinet))!;

        Assert.IsTrue(method.IsDefined(typeof(AuthorizeAttribute), true));
    }

    [TestMethod]
    public void ClassWithoutAuthorize_IsNotMarkedAuthorized()
    {
        Assert.IsFalse(typeof(ProtectedController).IsDefined(typeof(AuthorizeAttribute), true));
    }

    [TestMethod]
    public void ClassWithAuthorize_IsMarkedAuthorized()
    {
        Assert.IsTrue(typeof(RestrictedController).IsDefined(typeof(AuthorizeAttribute), true));
    }
}
