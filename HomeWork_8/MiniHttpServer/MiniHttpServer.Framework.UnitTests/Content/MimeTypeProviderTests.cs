using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Framework.Content;

namespace MiniHttpServer.Framework.UnitTests.Content;

[TestClass]
public class MimeTypeProviderTests
{
    [TestMethod]
    public void GetMimeType_HtmlExtension_ReturnsTextHtml()
    {
        var provider = new MimeTypeProvider();

        var type = provider.GetMimeType("index.html");
        
        Assert.AreEqual("text/html; charset=utf-8", type);
    }

    [TestMethod]
    public void GetMimeType_PngExtension_ReturnsImagePng()
    {
        var provider = new MimeTypeProvider();

        var type = provider.GetMimeType("photo.png");

        Assert.AreEqual("image/png", type);
    }

    [TestMethod]
    public void GetMimeType_CaseInsensitive_ReturnsSameType()
    {
        var provider = new MimeTypeProvider();

        var lower = provider.GetMimeType("style.css");
        var upper = provider.GetMimeType("STYLE.CSS");

        Assert.AreEqual(lower, upper);
        Assert.AreEqual("text/css; charset=utf-8", upper);
    }

    [TestMethod]
    public void GetMimeType_UnknownExtension_ReturnsOctetStream()
    {
        var provider = new MimeTypeProvider();

        var type = provider.GetMimeType("archive.zzz");

        Assert.AreEqual("application/octet-stream", type);
    }

    [TestMethod]
    public void GetMimeType_NoExtension_ReturnsOctetStream()
    {
        var provider = new MimeTypeProvider();

        var type = provider.GetMimeType("README");

        Assert.AreEqual("application/octet-stream", type);
    }
}
