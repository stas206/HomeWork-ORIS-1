using CustomHttpServer.Attributes;
using System.IO;

namespace CustomHttpServer.Controllers;

[HttpController("Dashboard")]
internal class DashboardController
{
    private readonly string _htmlFolder;

    public DashboardController(string htmlFolder)
    {
        _htmlFolder = htmlFolder;
    }

    // GET: Dashboard/profile?userId=1
    [Get("profile")]
    public string GetProfile(int userId)
    {
        string filePath = Path.Combine(_htmlFolder, "profile.html");

        if (File.Exists(filePath))
        {
            string html = File.ReadAllText(filePath);
            html = html.Replace("{{UserId}}", userId.ToString());
            return html;
        }

        return "<h1>404 - Profile not found</h1>";
    }

    // GET: Dashboard/activities?userId=5
    [Get("activities")]
    public string GetActivities(int userId)
    {
        string filePath = Path.Combine(_htmlFolder, "activities.html");

        if (File.Exists(filePath))
        {
            string html = File.ReadAllText(filePath);
            html = html.Replace("{{UserId}}", userId.ToString());
            return html;
        }

        return "<h1>404 - Activities not found</h1>";
    }
}