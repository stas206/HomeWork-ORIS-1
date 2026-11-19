using MiniHttpServer.Framework.UnitTests.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Specialized;
using System.Reflection;
using MiniHttpServer.Framework.Routing;

namespace MiniHttpServer.Framework.UnitTests.Routing;

[TestClass]
public class ModelBinderTests
{
    private static MethodInfo GetMethod(string name)
        => typeof(BinderFixture).GetMethod(name, BindingFlags.Public | BindingFlags.Static)!;

    [TestMethod]
    public void Bind_QueryParameters_FillsAllArguments()
    {
        var method = GetMethod(nameof(BinderFixture.Search));
        var values = new NameValueCollection
        {
            ["name"] = "Alex",
            ["age"] = "30"
        };

        var args = ModelBinder.Bind(method, values, out var errors);

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual("Alex", args[0]);
        Assert.AreEqual(30, args[1]);
    }

    [TestMethod]
    public void Bind_MissingRequiredValueType_ReportsErrorAndUsesDefault()
    {
        var method = GetMethod(nameof(BinderFixture.Search));
        var values = new NameValueCollection { ["name"] = "Alex" }; 

        var args = ModelBinder.Bind(method, values, out var errors);

        Assert.IsTrue(errors.Count > 0);
        Assert.IsTrue(errors.Any(e => e.Contains("age")));
        Assert.AreEqual(0, args[1]); 
    }

    [TestMethod]
    public void Bind_InvalidValueType_ReportsError()
    {
        var method = GetMethod(nameof(BinderFixture.Search));
        var values = new NameValueCollection { ["name"] = "Alex", ["age"] = "abc" };

        var args = ModelBinder.Bind(method, values, out var errors);

        Assert.IsTrue(errors.Count > 0);
        Assert.IsTrue(errors.Any(e => e.Contains("age")));
        Assert.AreEqual(0, args[1]);
    }

    [TestMethod]
    public void Bind_ParameterWithDefaultValue_UsesDefaultWhenMissing()
    {
        var method = GetMethod(nameof(BinderFixture.Paged)); 
        var values = new NameValueCollection { ["query"] = "dotnet" };

        var args = ModelBinder.Bind(method, values, out var errors);

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual("dotnet", args[0]);
        Assert.AreEqual(1, args[1]);
    }

    [TestMethod]
    public void Bind_BoolAccepts_On_Yes_One()
    {
        var method = GetMethod(nameof(BinderFixture.Toggle));
        var values = new NameValueCollection { ["enabled"] = "yes" };

        var args = ModelBinder.Bind(method, values, out var errors);

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(true, args[0]);
    }

    [TestMethod]
    public void Bind_BoolAccepts_Off_Zero_No()
    {
        var method = GetMethod(nameof(BinderFixture.Toggle));
        var values = new NameValueCollection { ["enabled"] = "0" };

        var args = ModelBinder.Bind(method, values, out var errors);

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(false, args[0]);
    }

    [TestMethod]
    public void Bind_InvalidBool_ReportsError()
    {
        var method = GetMethod(nameof(BinderFixture.Toggle));
        var values = new NameValueCollection { ["enabled"] = "maybe" };

        var args = ModelBinder.Bind(method, values, out var errors);

        Assert.IsTrue(errors.Count > 0);
        Assert.AreEqual(false, args[0]); 
    }

    [TestMethod]
    public void ParseForm_UrlEncodedBody_ParsesAllPairs()
    {
        var body = "name=Alex&age=30";

        var form = ModelBinder.ParseForm(body);

        Assert.AreEqual("Alex", form["name"]);
        Assert.AreEqual("30", form["age"]);
    }

    [TestMethod]
    public void ParseForm_PlusSign_DecodedAsSpace()
    {
        var body = "q=hello+world";

        var form = ModelBinder.ParseForm(body);

        Assert.AreEqual("hello world", form["q"]);
    }

    [TestMethod]
    public void ParseForm_PercentEncoded_DecodesValue()
    {
        var body = "q=100%25"; 

        var form = ModelBinder.ParseForm(body);

        Assert.AreEqual("100%", form["q"]);
    }

    [TestMethod]
    public void ParseForm_EmptyBody_ReturnsEmptyCollection()
    {
        var form = ModelBinder.ParseForm("");

        Assert.AreEqual(0, form.Count);
    }

    [TestMethod]
    public void ParseForm_KeyWithoutValue_GetsEmptyString()
    {
        var body = "flag";

        var form = ModelBinder.ParseForm(body);

        Assert.AreEqual(string.Empty, form["flag"]);
    }

    [TestMethod]
    public void Bind_NullMethod_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => ModelBinder.Bind(null!, new NameValueCollection(), out _));
    }
}
