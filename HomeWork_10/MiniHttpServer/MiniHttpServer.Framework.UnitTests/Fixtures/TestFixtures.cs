using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.HtmlTemplating;

namespace MiniHttpServer.Framework.UnitTests.Fixtures;


[Controller]
public sealed class AccountController : BaseController
{
    [Get]
    public IHttpResult Info() => new TextResult("info");

    [Post]
    public IHttpResult Login() => new TextResult("login");

    public IHttpResult InternalHelper() => new TextResult("helper");
}

[Controller]
public sealed class CatalogController : BaseController
{
    [Get]
    public IHttpResult List() => new TextResult("list");
}

public sealed class PlainService
{
    [Get]
    public IHttpResult ShouldNotBeFound() => new TextResult("nope");
}


public sealed class CounterService
{
    public int Value { get; set; }
}

[Controller]
public sealed class StatsController : BaseController
{
    public CounterService Counter { get; }

    public StatsController(CounterService counter)
    {
        Counter = counter ?? throw new ArgumentNullException(nameof(counter));
    }

    [Get]
    public IHttpResult Count() => new TextResult(Counter.Value.ToString());
}

[Controller]
public sealed class OptionalController : BaseController
{
    public string Name { get; }

    public OptionalController(string name = "fallback")
    {
        Name = name;
    }

    [Get]
    public IHttpResult Ping() => new TextResult(Name);
}

public static class BinderFixture
{
    public static IHttpResult Search(string name, int age) => new TextResult($"{name}:{age}");
    public static IHttpResult Paged(string query, int page = 1) => new TextResult($"{query}:{page}");
    public static IHttpResult Toggle(bool enabled) => new TextResult(enabled ? "on" : "off");
    public static IHttpResult Optional(string? name) => new TextResult(name ?? "-");
}

public sealed class StubTemplateRenderer : IHtmlTemplateRenderer
{
    public string RenderedHtml { get; set; } = "<html><body>stub</body></html>";
    public string? LastFilePath { get; private set; }
    public object? LastModel { get; private set; }
    public int RenderFromFileCalls { get; private set; }

    public string RenderFromString(string htmlTemplate, object? dataModel) => RenderedHtml;

    public string RenderFromFile(string filePath, object? dataModel)
    {
        RenderFromFileCalls++;
        LastFilePath = filePath;
        LastModel = dataModel;
        return RenderedHtml;
    }

    public string RenderToFile(string inputFilePath, string outputFilePath, object? dataModel)
        => RenderedHtml;
}
