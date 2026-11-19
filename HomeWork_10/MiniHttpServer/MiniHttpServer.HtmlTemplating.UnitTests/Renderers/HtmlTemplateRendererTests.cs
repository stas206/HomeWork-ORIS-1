using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using MiniHttpServer.HtmlTemplating;

namespace MiniHttpServer.HtmlTemplating.UnitTests.Renderers;

[TestClass]
public class HtmlTemplateRendererTests
{
    [TestMethod]
    public void Example_ValidCondition_Passes()
    {
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void RenderFromString_SingleProperty_ReplacesPlaceholder()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var template = "<h1>${Name}</h1>";
        var model = new UserModel { Name = "Alex" };

        var result = renderer.RenderFromString(template, model);

        Assert.AreEqual("<h1>Alex</h1>", result);
    }

    [TestMethod]
    public void RenderFromString_MultipleProperties_ReplacesAllPlaceholders()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var template = "<p>${Name}, ${Age}</p>";
        var model = new UserModel { Name = "Alex", Age = 30 };

        var result = renderer.RenderFromString(template, model);

        Assert.AreEqual("<p>Alex, 30</p>", result);
    }

    [TestMethod]
    public void RenderFromString_RepeatedProperty_ReplacesAllOccurrences()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var template = "${Name}-${Name}-${Name}";
        var model = new UserModel { Name = "X" };

        var result = renderer.RenderFromString(template, model);

        Assert.AreEqual("X-X-X", result);
    }

    [TestMethod]
    public void RenderFromString_NestedProperty_ResolvesPath()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var template = "${Address.City}, ${Address.Street}";
        var model = new UserModel
        {
            Address = new AddressModel { City = "Москва", Street = "Ленина" }
        };

        var result = renderer.RenderFromString(template, model);

        Assert.AreEqual("Москва, Ленина", result);
    }

    [TestMethod]
    public void RenderFromString_EmptyTemplate_ReturnsEmptyString()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var template = string.Empty;
        var model = new UserModel { Name = "Alex" };

        var result = renderer.RenderFromString(template, model);

        Assert.AreEqual(string.Empty, result);
    }


    [TestMethod]
    public void RenderFromString_IfTrue_RendersThenBlock()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var template = "$if(IsActive)активен$endif";
        var model = new UserModel { IsActive = true };

        var result = renderer.RenderFromString(template, model);

        Assert.AreEqual("активен", result);
    }

    [TestMethod]
    public void RenderFromString_IfFalse_RendersNothing()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var template = "$if(IsActive)активен$endif";
        var model = new UserModel { IsActive = false };

        var result = renderer.RenderFromString(template, model);

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void RenderFromString_IfElseFalse_RendersElseBlock()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var template = "$if(IsActive)[да]$else[нет]$endif";
        var model = new UserModel { IsActive = false };

        var result = renderer.RenderFromString(template, model);

        Assert.AreEqual("[нет]", result);
    }

    [TestMethod]
    public void RenderFromString_IfElseTrue_RendersThenBlock()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var template = "$if(IsActive)[да]$else[нет]$endif";
        var model = new UserModel { IsActive = true };

        var result = renderer.RenderFromString(template, model);

        Assert.AreEqual("[да]", result);
    }


    [TestMethod]
    public void RenderFromString_ForeachMultiple_RendersAllItems()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var template = "$foreach(var r in Roles)(${r})$endfor";
        var model = new UserModel { Roles = new() { "admin", "user" } };

        var result = renderer.RenderFromString(template, model);
        
        Assert.AreEqual("(admin)(user)", result);
    }

    [TestMethod]
    public void RenderFromString_ForeachEmpty_RendersNothing()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var template = "$foreach(var r in Roles)(${r})$endfor";
        var model = new UserModel { Roles = new() };

        var result = renderer.RenderFromString(template, model);

        Assert.AreEqual(string.Empty, result);
    }
    [TestMethod]
    public void RenderFromString_NestedIf_RendersMatchingBranch()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var template = "$if(IsActive)active$if(IsAdmin)admin$endif$endif";
        var model = new { IsActive = true, IsAdmin = true };

        var result = renderer.RenderFromString(template, model);

        Assert.AreEqual("activeadmin", result);
    }

    [TestMethod]
    public void RenderFromString_NestedIf_InnerFalse_RendersOnlyOuter()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var template = "$if(IsActive)active$if(IsAdmin)admin$endif$endif";
        var model = new { IsActive = true, IsAdmin = false };

        var result = renderer.RenderFromString(template, model);

        Assert.AreEqual("active", result);
    }

    [TestMethod]
    public void RenderFromString_NestedForeach_RendersAllCombinations()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var template = "$foreach(var p in Products)${p.Title}[$foreach(var t in p.Tags)${t}$endfor]$endfor";
        var model = new CatalogModel
        {
            Products = new()
            {
                new() { Title = "A", Tags = new() { "x", "y" } },
                new() { Title = "B", Tags = new() { "z" } }
            }
        };

        var result = renderer.RenderFromString(template, model);

        Assert.AreEqual("A[xy]B[z]", result);
    }

    [TestMethod]
    public void RenderFromString_ForeachWithIf_RendersConditionalItems()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var template = "$foreach(var p in Products)$if(p.InStock)${p.Title};$endif$endfor";
        var model = new CatalogModel
        {
            Products = new()
            {
                new() { Title = "A", InStock = true },
                new() { Title = "B", InStock = false },
                new() { Title = "C", InStock = true }
            }
        };

        var result = renderer.RenderFromString(template, model);

        Assert.AreEqual("A;C;", result);
    }

    [TestMethod]
    public void RenderFromString_ValueWithHtml_IsEncoded()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var template = "<div>${Name}</div>";
        var model = new UserModel { Name = "<b>опасно</b>" };

        var result = renderer.RenderFromString(template, model);

        Assert.AreEqual("<div>&lt;b&gt;опасно&lt;/b&gt;</div>", result);
    }

    [TestMethod]
    public void RenderFromFile_ValidFile_RendersTemplate()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "<h1>${Name}</h1>", Encoding.UTF8);
            var model = new UserModel { Name = "Bob" };

            var result = renderer.RenderFromFile(path, model);

            Assert.AreEqual("<h1>Bob</h1>", result);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public void RenderFromFile_MissingFile_ThrowsFileNotFoundException()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var path = Path.Combine(Path.GetTempPath(), "hw8_missing_" + Guid.NewGuid() + ".html");

        Assert.ThrowsException<FileNotFoundException>(() => renderer.RenderFromFile(path, null));
    }

    [TestMethod]
    public void RenderToFile_ValidInput_WritesRenderedFile()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var inputPath = Path.GetTempFileName();
        var outputPath = Path.Combine(Path.GetTempPath(), "hw8_out_" + Guid.NewGuid() + ".html");
        try
        {
            File.WriteAllText(inputPath, "<p>${Name}=${Age}</p>", Encoding.UTF8);
            var model = new UserModel { Name = "Kim", Age = 25 };

            renderer.RenderToFile(inputPath, outputPath, model);

            Assert.IsTrue(File.Exists(outputPath));
            Assert.AreEqual("<p>Kim=25</p>", File.ReadAllText(outputPath));
        }
        finally
        {
            if (File.Exists(inputPath)) File.Delete(inputPath);
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [TestMethod]
    public void RenderFromString_UnclosedIf_ThrowsSyntaxException()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var template = "$if(IsActive)без завершения";
        var model = new UserModel { IsActive = true };

        Assert.ThrowsException<TemplateSyntaxException>(
            () => renderer.RenderFromString(template, model));
    }

    [TestMethod]
    public void RenderFromString_ElseWithoutIf_ThrowsSyntaxException()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var template = "текст $else ветка";

        Assert.ThrowsException<TemplateSyntaxException>(
            () => renderer.RenderFromString(template, new UserModel()));
    }

    [TestMethod]
    public void RenderFromString_UnknownProperty_ThrowsSyntaxException()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var template = "$if(Missing)X$endif";

        Assert.ThrowsException<TemplateSyntaxException>(
            () => renderer.RenderFromString(template, new UserModel()));
    }

    [TestMethod]
    public void RenderFromString_BadForEachHeader_ThrowsSyntaxException()
    {
        IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
        var template = "$foreach(неверный синтаксис)X$endfor";

        Assert.ThrowsException<TemplateSyntaxException>(
            () => renderer.RenderFromString(template, new UserModel()));
    }
}
