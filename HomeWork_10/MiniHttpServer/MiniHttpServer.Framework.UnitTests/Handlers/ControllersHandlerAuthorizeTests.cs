using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Sockets;
using MiniHttpServer.Framework.Authentication;
using MiniHttpServer.Framework.Handlers;
using MiniHttpServer.Framework.Routing;
using MiniHttpServer.Framework.UnitTests.Fixtures;

namespace MiniHttpServer.Framework.UnitTests.Handlers;

[TestClass]
[DoNotParallelize]
public class ControllersHandlerAuthorizeTests
{
    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task<(HttpStatusCode status, string body)> SendAsync(
        ControllersHandler handler, string method, string path, string? cookie = null)
    {
        var port = GetFreePort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        var acceptTask = Task.Run(async () =>
        {
            var ctx = await listener.GetContextAsync();
            await handler.HandleAsync(ctx);
        });

        using var httpHandler = new HttpClientHandler { UseCookies = false };
        using var client = new HttpClient(httpHandler);
        var request = new HttpRequestMessage(new HttpMethod(method), $"http://127.0.0.1:{port}{path}");
        if (cookie is not null)
        {
            request.Headers.Add("Cookie", cookie);
        }

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        await acceptTask;
        listener.Stop();
        listener.Close();

        return (response.StatusCode, body);
    }

    private static ControllersHandler CreateHandler(IUserSessionService sessionService)
    {
        var factory = new DefaultControllerFactory();
        var renderer = new StubTemplateRenderer();
        return new ControllersHandler(
            typeof(ProtectedController).Assembly, factory, renderer, sessionService);
    }

    [TestMethod]
    public async Task OpenMethod_NoAuthorizationRequired_Returns200()
    {
        var handler = CreateHandler(new FakeSessionService());

        var (status, _) = await SendAsync(handler, "GET", "/protected/open");

        Assert.AreEqual(HttpStatusCode.OK, status);
    }

    [TestMethod]
    public async Task ProtectedMethod_WithoutCookie_Returns401()
    {
        var handler = CreateHandler(new FakeSessionService());

        var (status, _) = await SendAsync(handler, "GET", "/protected/cabinet");

        Assert.AreEqual(HttpStatusCode.Unauthorized, status);
    }

    [TestMethod]
    public async Task ProtectedMethod_WithValidSession_Returns200()
    {
        var handler = CreateHandler(new FakeSessionService());

        var (status, body) = await SendAsync(handler, "GET", "/protected/cabinet", "sessionId=session-ok");

        Assert.AreEqual(HttpStatusCode.OK, status);
        Assert.IsTrue(body.Contains("cabinet"));
    }

    [TestMethod]
    public async Task ProtectedMethod_WithUnknownSession_Returns401()
    {
        var handler = CreateHandler(new FakeSessionService());

        var (status, _) = await SendAsync(handler, "GET", "/protected/cabinet", "sessionId=unknown");

        Assert.AreEqual(HttpStatusCode.Unauthorized, status);
    }

    [TestMethod]
    public async Task ClassLevelAuthorize_WithoutCookie_Returns401()
    {
        var handler = CreateHandler(new FakeSessionService());

        var (status, _) = await SendAsync(handler, "GET", "/restricted/secret");

        Assert.AreEqual(HttpStatusCode.Unauthorized, status);
    }

    [TestMethod]
    public async Task ClassLevelAuthorize_WithValidSession_Returns200()
    {
        var handler = CreateHandler(new FakeSessionService());

        var (status, body) = await SendAsync(handler, "GET", "/restricted/secret", "sessionId=session-ok");

        Assert.AreEqual(HttpStatusCode.OK, status);
        Assert.IsTrue(body.Contains("secret"));
    }
}
