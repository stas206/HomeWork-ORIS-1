using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.ORM;

namespace MiniHttpServer.ORM.UnitTests;

[TestClass]
public class SqlIdentifiersTests
{
    [TestMethod]
    public void Escape_ValidIdentifier_ReturnsQuotedName()
    {
        var result = SqlIdentifiers.Escape("Users");

        Assert.AreEqual("\"Users\"", result);
    }

    [TestMethod]
    public void Escape_ValidIdentifierWithUnderscore_ReturnsQuotedName()
    {
        var result = SqlIdentifiers.Escape("order_items");

        Assert.AreEqual("\"order_items\"", result);
    }

    [TestMethod]
    [DataRow("User; DROP TABLE Users")]
    [DataRow("1table")]
    [DataRow("na me")]
    [DataRow("bad-name")]
    [DataRow("")]
    [DataRow("   ")]
    public void Escape_InvalidIdentifier_Throws(string identifier)
    {
        Assert.ThrowsException<ArgumentException>(() => SqlIdentifiers.Escape(identifier));
    }

    [TestMethod]
    public void Escape_NullIdentifier_Throws()
    {
        Assert.ThrowsException<ArgumentException>(() => SqlIdentifiers.Escape(null!));
    }
}
