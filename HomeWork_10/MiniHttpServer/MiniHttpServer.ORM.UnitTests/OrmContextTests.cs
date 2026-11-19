using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.ORM;

namespace MiniHttpServer.ORM.UnitTests;

[TestClass]
public class OrmContextTests
{
    [TestMethod]
    public void Constructor_NullConnectionString_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new OrmContext(null!));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_EmptyOrWhitespaceConnectionString_Throws(string connectionString)
    {
        Assert.ThrowsException<ArgumentException>(() => new OrmContext(connectionString));
    }

    [TestMethod]
    public void Constructor_ValidConnectionString_CreatesInstance()
    {
        var orm = new OrmContext("Host=localhost;Port=5432;Database=orisdb;Username=postgres;Password=postgres;");

        Assert.IsInstanceOfType(orm, typeof(IOrmContext));
    }
}
