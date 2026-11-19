using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.ORM;

namespace MiniHttpServer.ORM.UnitTests;

[TestClass]
public class SqlValueConverterTests
{
    [TestMethod]
    public void Convert_Int32_ReturnsSameInt()
    {
        var result = SqlValueConverter.Convert(42, typeof(int));

        Assert.IsInstanceOfType(result, typeof(int));
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void Convert_String_ReturnsSameString()
    {
        var result = SqlValueConverter.Convert("Alex", typeof(string));

        Assert.AreEqual("Alex", result);
    }

    [TestMethod]
    public void Convert_LongToInt_ChangesType()
    {
        var result = SqlValueConverter.Convert(5L, typeof(int));

        Assert.IsInstanceOfType(result, typeof(int));
        Assert.AreEqual(5, result);
    }

    [TestMethod]
    public void Convert_StringToDecimal_ParsesInvariant()
    {
        var result = SqlValueConverter.Convert("12.5", typeof(decimal));

        Assert.AreEqual(12.5m, result);
    }

    [TestMethod]
    public void Convert_IntToEnum_ReturnsEnumValue()
    {
        var result = SqlValueConverter.Convert(2, typeof(UserRole));

        Assert.AreEqual(UserRole.Admin, result);
    }

    [TestMethod]
    public void Convert_NullOnNonNullableValueType_ReturnsTypeDefault()
    {
        var result = SqlValueConverter.Convert(null, typeof(int));

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void Convert_DBNullOnNonNullableValueType_ReturnsTypeDefault()
    {
        var result = SqlValueConverter.Convert(DBNull.Value, typeof(int));

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void Convert_NullOnNullableValueType_ReturnsNull()
    {
        var result = SqlValueConverter.Convert(null, typeof(int?));

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Convert_NullOnReferenceType_ReturnsNull()
    {
        var result = SqlValueConverter.Convert(null, typeof(string));

        Assert.IsNull(result);
    }
}
