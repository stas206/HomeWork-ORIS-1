using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Text;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.Framework.UnitTests.Fixtures;

namespace MiniHttpServer.Framework.UnitTests.Results;

[TestClass]
public class PageResultTests
{
    [TestMethod]
    public void BuildResponseContent_WithStubRenderer_ReturnsRenderedHtml()
    {
        var stub = new StubTemplateRenderer { RenderedHtml = "<h1>rendered</h1>" };
        var path = Path.GetTempFileName(); // абсолютный путь к существующему файлу
        var model = new { Title = "T" };
        try
        {
            var result = new PageResult(stub, path, model);

            var content = result.BuildResponseContent();

            Assert.AreEqual("text/html; charset=utf-8", content.ContentType);
            Assert.AreEqual("<h1>rendered</h1>", Encoding.UTF8.GetString(content.Body));
            Assert.AreEqual(1, stub.RenderFromFileCalls);
            Assert.AreEqual(path, stub.LastFilePath);
            Assert.AreSame(model, stub.LastModel);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public void BuildResponseContent_PreservesCustomStatusCode()
    {
        var stub = new StubTemplateRenderer();
        var path = Path.GetTempFileName();
        try
        {
            var result = new PageResult(stub, path, null, HttpStatusCode.Created);

            var content = result.BuildResponseContent();

            Assert.AreEqual(HttpStatusCode.Created, content.StatusCode);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public void Constructor_NullRenderer_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new PageResult(null!, "Templates/User/Profile.html", null));
    }

    [TestMethod]
    public void Constructor_NullTemplatePath_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new PageResult(new StubTemplateRenderer(), null!, null));
    }
}
