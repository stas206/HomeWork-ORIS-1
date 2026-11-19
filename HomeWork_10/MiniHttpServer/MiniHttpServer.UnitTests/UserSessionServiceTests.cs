using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Services.Authentication;

namespace MiniHttpServer.UnitTests;

[TestClass]
public class UserSessionServiceTests
{
    [TestMethod]
    public void CreateSession_ReturnsNonEmptyIdentifier()
    {
        var service = new UserSessionService();

        var sessionId = service.CreateSession(1);

        Assert.IsFalse(string.IsNullOrWhiteSpace(sessionId));
    }

    [TestMethod]
    public void GetUserId_ValidSession_ReturnsUserId()
    {
        var service = new UserSessionService();

        var sessionId = service.CreateSession(42);

        Assert.AreEqual(42, service.GetUserId(sessionId));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("unknown-session")]
    public void GetUserId_AbsentOrInvalidSession_ReturnsNull(string? sessionId)
    {
        var service = new UserSessionService();
        service.CreateSession(1);

        Assert.IsNull(service.GetUserId(sessionId));
    }

    [TestMethod]
    public async Task GetUserId_ExpiredSession_ReturnsNull()
    {
        var service = new UserSessionService(TimeSpan.FromMilliseconds(300));

        var sessionId = service.CreateSession(1);

        Assert.AreEqual(1, service.GetUserId(sessionId));

        await Task.Delay(450);

        Assert.IsNull(service.GetUserId(sessionId));
    }

    [TestMethod]
    public void RemoveSession_RemovesActiveSession()
    {
        var service = new UserSessionService();
        var sessionId = service.CreateSession(7);

        Assert.AreEqual(7, service.GetUserId(sessionId));

        service.RemoveSession(sessionId);

        Assert.IsNull(service.GetUserId(sessionId));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("absent")]
    public void RemoveSession_AbsentOrInvalid_DoesNotThrow(string? sessionId)
    {
        var service = new UserSessionService();

        service.RemoveSession(sessionId);
    }

    [TestMethod]
    public void CreateSession_MultipleCalls_ReturnsUniqueIdentifiers()
    {
        var service = new UserSessionService();
        var ids = new HashSet<string>();

        for (int i = 0; i < 100; i++)
        {
            var sessionId = service.CreateSession(i);

            Assert.IsTrue(ids.Add(sessionId));
            Assert.AreEqual(i, service.GetUserId(sessionId));
        }
    }
}
