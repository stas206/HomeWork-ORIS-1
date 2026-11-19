using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using MiniHttpServer.Controllers;
using MiniHttpServer.Framework.Authentication;
using MiniHttpServer.Framework.Handlers;
using MiniHttpServer.Framework.Routing;
using MiniHttpServer.ORM;
using MiniHttpServer.Services;
using MiniHttpServer.Services.Authentication;
using MiniHttpServer.UnitTests.Fixtures;

namespace MiniHttpServer.UnitTests;

[TestClass]
[DoNotParallelize]
public class AuthenticationEndToEndTests
{
    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static ControllersHandler CreateHandler(IUserSessionService sessionService)
    {
        var factory = new DefaultControllerFactory();
        factory.Register<IEmailService>(() => new FakeEmailService());
        factory.Register<IOrmContext>(() => new StubOrmContext());

        return new ControllersHandler(
            typeof(AuthController).Assembly,
            factory,
            new StubTemplateRenderer(),
            sessionService);
    }

    private static async Task<(HttpStatusCode status, string body, bool hasSetCookie)> SendAsync(
        ControllersHandler handler,
        HttpClient client,
        HttpMethod method,
        string path,
        HttpContent? content = null)
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

        using var request = new HttpRequestMessage(method, $"http://127.0.0.1:{port}{path}")
        {
            Content = content
        };

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        var hasSetCookie = response.Headers.Contains("Set-Cookie");

        await acceptTask;
        listener.Stop();
        listener.Close();

        return (response.StatusCode, body, hasSetCookie);
    }

    private static HttpClient CreateClientWithContainer(CookieContainer container)
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = container,
            UseCookies = true
        };
        return new HttpClient(handler);
    }

    [TestMethod]
    public async Task FullFlow_Login_Profile_Logout_ThenProfileUnauthorized()
    {
        var sessionService = new UserSessionService();
        var handler = CreateHandler(sessionService);
        var container = new CookieContainer();
        using var client = CreateClientWithContainer(container);

        var (statusBeforeLogin, _, _) = await SendAsync(handler, client, HttpMethod.Get, "/user/profile");
        Assert.AreEqual(HttpStatusCode.Unauthorized, statusBeforeLogin);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["login"] = "student",
            ["password"] = "test123"
        });
        var (loginStatus, loginBody, loginSetCookie) =
            await SendAsync(handler, client, HttpMethod.Post, "/auth/login", form);
        Assert.AreEqual(HttpStatusCode.OK, loginStatus);
        Assert.IsTrue(loginSetCookie);
        Assert.IsTrue(loginBody.Contains("Вход выполнен"));

        var (profileStatus, profileBody, _) =
            await SendAsync(handler, client, HttpMethod.Get, "/user/profile");
        Assert.AreEqual(HttpStatusCode.OK, profileStatus);
        Assert.IsTrue(profileBody.Contains("Личный кабинет"));

        var (logoutStatus, logoutBody, _) =
            await SendAsync(handler, client, HttpMethod.Post, "/auth/logout");
        Assert.AreEqual(HttpStatusCode.OK, logoutStatus);
        Assert.IsTrue(logoutBody.Contains("Выход выполнен"));

        var (statusAfterLogout, _, _) = await SendAsync(handler, client, HttpMethod.Get, "/user/profile");
        Assert.AreEqual(HttpStatusCode.Unauthorized, statusAfterLogout);
    }

    [TestMethod]
    public async Task Login_WithWrongCredentials_Returns401AndNoCookie()
    {
        var sessionService = new UserSessionService();
        var handler = CreateHandler(sessionService);
        var container = new CookieContainer();
        using var client = CreateClientWithContainer(container);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["login"] = "student",
            ["password"] = "wrong"
        });
        var (status, body, hasSetCookie) =
            await SendAsync(handler, client, HttpMethod.Post, "/auth/login", form);

        Assert.AreEqual(HttpStatusCode.Unauthorized, status);
        Assert.IsFalse(hasSetCookie);
        Assert.IsTrue(body.Contains("Неверный логин или пароль"));
    }

    [TestMethod]
    public async Task Profile_WithUnknownSessionCookie_Returns401()
    {
        var sessionService = new UserSessionService();
        var handler = CreateHandler(sessionService);
        var container = new CookieContainer();
        container.Add(new Cookie("sessionId", "does-not-exist", "/", "127.0.0.1"));
        using var client = CreateClientWithContainer(container);

        var (status, _, _) = await SendAsync(handler, client, HttpMethod.Get, "/user/profile");

        Assert.AreEqual(HttpStatusCode.Unauthorized, status);
    }

    [TestMethod]
    public async Task CookieController_Set_Get_Delete_RoundTrip()
    {
        var sessionService = new UserSessionService();
        var handler = CreateHandler(sessionService);
        var container = new CookieContainer();
        using var client = CreateClientWithContainer(container);

        var (setStatus, _, setCookie) = await SendAsync(handler, client, HttpMethod.Get, "/cookie/set");
        Assert.AreEqual(HttpStatusCode.OK, setStatus);
        Assert.IsTrue(setCookie);

        var (getStatus, getBody, _) = await SendAsync(handler, client, HttpMethod.Get, "/cookie/get");
        Assert.AreEqual(HttpStatusCode.OK, getStatus);
        Assert.IsTrue(getBody.Contains("HelloWorld"));

        var (deleteStatus, _, deleteCookie) = await SendAsync(handler, client, HttpMethod.Get, "/cookie/delete");
        Assert.AreEqual(HttpStatusCode.OK, deleteStatus);
        Assert.IsTrue(deleteCookie);

        var (afterDeleteStatus, afterDeleteBody, _) =
            await SendAsync(handler, client, HttpMethod.Get, "/cookie/get");
        Assert.AreEqual(HttpStatusCode.OK, afterDeleteStatus);

        using var doc = JsonDocument.Parse(afterDeleteBody);
        Assert.IsTrue(doc.RootElement.TryGetProperty("Value", out var valueProperty));
        Assert.AreEqual(JsonValueKind.Null, valueProperty.ValueKind);
    }
}
