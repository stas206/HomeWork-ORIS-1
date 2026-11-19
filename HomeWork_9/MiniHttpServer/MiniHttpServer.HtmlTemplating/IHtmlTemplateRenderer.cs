namespace MiniHttpServer.HtmlTemplating;

public interface IHtmlTemplateRenderer
{
    string RenderFromString(string htmlTemplate, object? dataModel);

    string RenderFromFile(string filePath, object? dataModel);

    string RenderToFile(
        string inputFilePath,
        string outputFilePath,
        object? dataModel);
}
