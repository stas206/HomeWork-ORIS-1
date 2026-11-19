namespace MiniHttpServer.ORM;

public sealed record ParameterData(string Name, object? Value);

public sealed record CommandData(string Text, IReadOnlyList<ParameterData> Parameters);
