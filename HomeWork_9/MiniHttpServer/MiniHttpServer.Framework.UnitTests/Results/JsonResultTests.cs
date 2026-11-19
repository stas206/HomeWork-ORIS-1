using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Text;
using MiniHttpServer.Framework.Results;

namespace MiniHttpServer.Framework.UnitTests.Results;

[TestClass]
public class JsonResultTests
{
    [TestMethod]
    public void BuildResponseContent_SetsJsonContentTypeAndOkStatus()
    {
        var result = new JsonResult(new { Name = "Alex", Age = 30 });

        var content = result.BuildResponseContent();

        Assert.AreEqual(HttpStatusCode.OK, content.StatusCode);
        Assert.AreEqual("application/json; charset=utf-8", content.ContentType);
    }

    [TestMethod]
    public void BuildResponseContent_SerializesObjectToJsonBody()
    {
        var result = new JsonResult(new { Name = "Alex", Age = 30 });

        var content = result.BuildResponseContent();
        var body = Encoding.UTF8.GetString(content.Body);

        Assert.AreEqual("""{"Name":"Alex","Age":30}""", body);
    }

    [TestMethod]
    public void BuildResponseContent_CyrillicNotEscaped()
    {
        var result = new JsonResult(new { City = "Москва" });

        var body = Encoding.UTF8.GetString(result.BuildResponseContent().Body);

        Assert.IsTrue(body.Contains("Москва"));
        Assert.IsFalse(body.Contains("\\u"));
    }

    [TestMethod]
    public void BuildResponseContent_CustomStatusCode_Preserved()
    {
        var result = new JsonResult(new { ok = false }, HttpStatusCode.BadRequest);

        var content = result.BuildResponseContent();

        Assert.AreEqual(HttpStatusCode.BadRequest, content.StatusCode);
    }

    [TestMethod]
    public void Constructor_NullData_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new JsonResult(null!));
    }
}
