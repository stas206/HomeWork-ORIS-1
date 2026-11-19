using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Sockets;
using MiniHttpServer.Framework.Handlers;

namespace MiniHttpServer.Framework.UnitTests.Handlers;

[TestClass]
[DoNotParallelize]
public class RequestHandlerChainTests
{
    private sealed class PassThroughHandler : RequestHandler
    {
        public override Task HandleAsync(HttpListenerContext context) => HandleNextAsync(context);
    }

    private sealed class RecordingHandler : RequestHandler
    {
        public bool WasCalled { get; private set; }
        public override Task HandleAsync(HttpListenerContext context)
        {
            WasCalled = true;
            return HandleNextAsync(context);
        }
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task<(HttpStatusCode status, string body)> RunOnceAsync(
        RequestHandler handler, string method, string path)
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

        using var client = new HttpClient();
        var request = new HttpRequestMessage(new HttpMethod(method), $"http://127.0.0.1:{port}{path}");
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        await acceptTask;
        listener.Stop();
        listener.Close();

        return (response.StatusCode, body);
    }

    [TestMethod]
    public async Task Chain_PassesControlToNextHandler()
    {
        var recorder = new RecordingHandler();
        var passThrough = new PassThroughHandler();
        var notFound = new NotFoundHandler();
        passThrough.SetNext(recorder);
        recorder.SetNext(notFound);

        var (status, _) = await RunOnceAsync(passThrough, "GET", "/anything");

        Assert.IsTrue(recorder.WasCalled);
        Assert.AreEqual(HttpStatusCode.NotFound, status);
    }

    [TestMethod]
    public async Task NotFoundHandler_Returns404WithHtmlBody()
    {
        var handler = new NotFoundHandler();

        var (status, body) = await RunOnceAsync(handler, "GET", "/missing");

        Assert.AreEqual(HttpStatusCode.NotFound, status);
        Assert.IsTrue(body.Contains("404"));
    }
}
