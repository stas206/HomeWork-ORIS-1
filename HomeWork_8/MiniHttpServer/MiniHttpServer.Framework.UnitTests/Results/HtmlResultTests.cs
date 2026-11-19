using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Text;
using MiniHttpServer.Framework.Results;

namespace MiniHttpServer.Framework.UnitTests.Results;

[TestClass]
public class HtmlResultTests
{
    [TestMethod]
    public void BuildResponseContent_SetsHtmlContentTypeAndOkStatus()
    {
        var result = new HtmlResult("<h1>Hello</h1>");

        var content = result.BuildResponseContent();

        Assert.AreEqual(HttpStatusCode.OK, content.StatusCode);
        Assert.AreEqual("text/html; charset=utf-8", content.ContentType);
    }

    [TestMethod]
    public void BuildResponseContent_BodyContainsHtmlAsUtf8()
    {
        var html = "<p>Привет, мир!</p>";
        var result = new HtmlResult(html);

        var content = result.BuildResponseContent();
        var body = Encoding.UTF8.GetString(content.Body);

        Assert.AreEqual(html, body);
    }

    [TestMethod]
    public void BuildResponseContent_CustomStatusCode_Preserved()
    {
        var result = new HtmlResult("<h1>Created</h1>", HttpStatusCode.Created);

        var content = result.BuildResponseContent();

        Assert.AreEqual(HttpStatusCode.Created, content.StatusCode);
    }

    [TestMethod]
    public void Constructor_NullHtml_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new HtmlResult(null!));
    }
}
