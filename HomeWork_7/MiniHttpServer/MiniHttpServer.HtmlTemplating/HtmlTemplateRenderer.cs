using System.Collections;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniHttpServer.HtmlTemplating;

public sealed class HtmlTemplateRenderer : IHtmlTemplateRenderer
{
    public string RenderFromString(string htmlTemplate, object? dataModel)
    {
        ArgumentNullException.ThrowIfNull(htmlTemplate);
        return new TemplateEngine(htmlTemplate, dataModel).Render();
    }

    public string RenderFromFile(string filePath, object? dataModel)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Путь к файлу шаблона не задан.", nameof(filePath));
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Файл шаблона не найден: {filePath}", filePath);

        var template = File.ReadAllText(filePath);
        return RenderFromString(template, dataModel);
    }

    public string RenderToFile(string inputFilePath, string outputFilePath, object? dataModel)
    {
        if (string.IsNullOrWhiteSpace(outputFilePath))
            throw new ArgumentException("Путь к выходному файлу не задан.", nameof(outputFilePath));

        var result = RenderFromFile(inputFilePath, dataModel);

        var dir = Path.GetDirectoryName(Path.GetFullPath(outputFilePath));
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(outputFilePath, result, Encoding.UTF8);
        return result;
    }

    private sealed class TemplateEngine
    {
        private static readonly Regex _directiveRegex = new(
            @"\$\{(?<var>[^}]+)\}" +                       
            @"|\$if\s*\(\s*(?<if>[^)]*)\s*\)" +           
            @"|\$foreach\s*\((?<foreach>[^)]*)\)" +       
            @"|\$else\b" +                                
            @"|\$endif\b" +                               
            @"|\$endfor\b",                               
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly string _template;
        private readonly object _model;

        public TemplateEngine(string template, object? model)
        {
            _template = template;
            _model = model ?? new object();
        }

        public string Render()
        {
            var nodes = Parse(_template);
            var context = new RenderContext(_model);
            return RenderNodes(nodes, context);
        }


        private List<Node> Parse(string template)
        {
            var root = new List<Node>();
            var stack = new Stack<BlockFrame>();
            var current = root;
            int lastEnd = 0;

            foreach (Match m in _directiveRegex.Matches(template))
            {
                int textStart = lastEnd;
                int textEnd = m.Index;
                if (textEnd > textStart)
                {
                    var literal = template.Substring(textStart, textEnd - textStart);
                    current.Add(new TextNode(literal));
                }

                bool isBlockDirective =
                    m.Groups["if"].Success || m.Groups["foreach"].Success ||
                    m.Value is "$else" or "$endif" or "$endfor";

                lastEnd = m.Index + m.Length;
                if (isBlockDirective)
                {
                    lastEnd = SkipTrailingLineIfAlone(template, m.Index, lastEnd);
                }

                if (m.Groups["var"].Success)
                {
                    var path = m.Groups["var"].Value.Trim();
                    if (path.Length == 0)
                        throw new TemplateSyntaxException("Пустое выражение подстановки '${}'.");
                    current.Add(new VariableNode(path));
                }
                else if (m.Groups["if"].Success)
                {
                    var condition = m.Groups["if"].Value.Trim();
                    if (condition.Length == 0)
                        throw new TemplateSyntaxException("Конструкция $if требует условия: '$if(PropertyName)'.");
                    var node = new IfNode(condition);
                    current.Add(node);
                    stack.Push(new BlockFrame(node, current, isIf: true));
                    current = node.Then;
                }
                else if (m.Groups["foreach"].Success)
                {
                    var (itemName, collectionPath) = ParseForEachHeader(m.Groups["foreach"].Value);
                    var node = new ForEachNode(itemName, collectionPath);
                    current.Add(node);
                    stack.Push(new BlockFrame(node, current, isIf: false));
                    current = node.Body;
                }
                else if (m.Value is "$else")
                {
                    if (stack.Count == 0 || !stack.Peek().IsIf)
                        throw new TemplateSyntaxException("Конструкция $else встречена без открытого $if.");
                    var frame = stack.Peek();
                    current = frame.IfNode!.Else;
                }
                else if (m.Value is "$endif")
                {
                    if (stack.Count == 0 || !stack.Peek().IsIf)
                        throw new TemplateSyntaxException("Конструкция $endif встречена без открытого $if.");
                    var frame = stack.Pop();
                    current = frame.Parent;
                }
                else if (m.Value is "$endfor")
                {
                    if (stack.Count == 0 || stack.Peek().IsIf)
                        throw new TemplateSyntaxException("Конструкция $endfor встречена без открытого $foreach.");
                    var frame = stack.Pop();
                    current = frame.Parent;
                }
            }

            if (lastEnd < template.Length)
                root.Add(new TextNode(template.Substring(lastEnd)));

            if (stack.Count > 0)
            {
                var top = stack.Peek();
                var missing = top.IsIf ? "$endif" : "$endfor";
                var what = top.IsIf ? "условия $if" : "цикла $foreach";
                throw new TemplateSyntaxException($"Не закрыта конструкция {what}: отсутствует {missing}.");
            }

            return root;
        }

        private static int SkipTrailingLineIfAlone(string template, int directiveStart, int end)
        {
            int lineStart = directiveStart;
            while (lineStart > 0 && template[lineStart - 1] != '\n' && template[lineStart - 1] != '\r')
                lineStart--;
            for (int i = lineStart; i < directiveStart; i++)
                if (!char.IsWhiteSpace(template[i]))
                    return end;

            int j = end;
            while (j < template.Length && template[j] != '\n' && template[j] != '\r')
            {
                if (!char.IsWhiteSpace(template[j]))
                    return end;
                j++;
            }

            if (j < template.Length)
            {
                if (template[j] == '\r' && j + 1 < template.Length && template[j + 1] == '\n')
                    return j + 2;
                return j + 1;
            }
            return j;
        }

        private static (string itemName, string collectionPath) ParseForEachHeader(string header)
        {
            var text = header.Trim();
            if (text.Length == 0)
                throw new TemplateSyntaxException("Заголовок цикла $foreach пуст: ожидается '$foreach(var item in Collection)'.");

            var match = Regex.Match(text,
                @"^var\s+(?<item>[A-Za-z_]\w*)\s+in\s+(?<coll>.+)$",
                RegexOptions.CultureInvariant);
            if (!match.Success)
                throw new TemplateSyntaxException(
                    $"Некорректный заголовок цикла $foreach: '{header}'. " +
                    "Ожидается синтаксис '$foreach(var item in Collection)'.");

            var itemName = match.Groups["item"].Value;
            var collectionPath = match.Groups["coll"].Value.Trim();
            if (collectionPath.Length == 0)
                throw new TemplateSyntaxException("В цикле $foreach не указана коллекция для перебора.");

            return (itemName, collectionPath);
        }


        private string RenderNodes(List<Node> nodes, RenderContext context)
        {
            if (nodes.Count == 0) return string.Empty;
            if (nodes.Count == 1) return RenderNode(nodes[0], context);

            var sb = new StringBuilder();
            foreach (var node in nodes)
                sb.Append(RenderNode(node, context));
            return sb.ToString();
        }

        private string RenderNode(Node node, RenderContext context)
        {
            switch (node)
            {
                case TextNode text:
                    return text.Text;
                case VariableNode variable:
                    var value = ResolveValue(variable.Path, context);
                    return FormatValue(value);
                case IfNode ifNode:
                    var conditionValue = ResolveValue(ifNode.Condition, context);
                    return IsTruthy(conditionValue)
                        ? RenderNodes(ifNode.Then, context)
                        : RenderNodes(ifNode.Else, context);
                case ForEachNode forEach:
                    return RenderForEach(forEach, context);
                default:
                    return string.Empty;
            }
        }

        private string RenderForEach(ForEachNode node, RenderContext context)
        {
            var collection = ResolveValue(node.CollectionPath, context);

            if (collection is null)
                return string.Empty; 

            if (collection is not IEnumerable enumerable)
                throw new TemplateSyntaxException(
                    $"Значение '{node.CollectionPath}' не является коллекцией и не может быть перебрано в $foreach.");

            var sb = new StringBuilder();
            foreach (var item in enumerable)
            {
                context.Push(node.ItemName, item);
                try
                {
                    sb.Append(RenderNodes(node.Body, context));
                }
                finally
                {
                    context.Pop();
                }
            }
            return sb.ToString();
        }

        private static string FormatValue(object? value)
        {
            if (value is null) return string.Empty;
            var text = ConvertToString(value);
            return WebUtility.HtmlEncode(text);
        }

        private static string ConvertToString(object value)
        {
            return value switch
            {
                string s => s,
                bool b => b ? "True" : "False",
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? string.Empty
            };
        }

        private static bool IsTruthy(object? value)
        {
            if (value is null) return false;
            if (value is bool b) return b;
            if (value is string s) return !string.IsNullOrWhiteSpace(s);
            if (value is IEnumerable enumerable)
            {
                var e = enumerable.GetEnumerator();
                try { return e.MoveNext(); }
                finally { (e as IDisposable)?.Dispose(); }
            }
            if (value is IConvertible convertible)
            {
                try { return Convert.ToDouble(convertible, CultureInfo.InvariantCulture) != 0; }
                catch { return true; }
            }
            return true;
        }

        private object? ResolveValue(string path, RenderContext context)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var parts = path.Split('.');
            for (int i = 0; i < parts.Length; i++)
                parts[i] = parts[i].Trim();

            var first = parts[0];
            if (first.Length == 0)
                throw new TemplateSyntaxException($"Некорректный путь выражения '{path}'.");

            if (context.TryGetVariable(first, out var current))
            {
                for (int i = 1; i < parts.Length; i++)
                {
                    if (current is null)
                        throw new TemplateSyntaxException(
                            $"Не удалось получить свойство '{parts[i]}': объект равен null (путь '{path}').");
                    current = GetMember(current, parts[i], path);
                }
                return current;
            }

            current = context.Root;
            for (int i = 0; i < parts.Length; i++)
            {
                if (current is null)
                    throw new TemplateSyntaxException(
                        $"Не удалось получить свойство '{parts[i]}': объект равен null (путь '{path}').");
                current = GetMember(current, parts[i], path);
            }
            return current;
        }

        private static object? GetMember(object target, string name, string fullPath)
        {
            var type = target.GetType();

            var prop = type.GetProperty(name,
                BindingFlags.Public | BindingFlags.Instance);
            if (prop is not null) return prop.GetValue(target);

            var field = type.GetField(name,
                BindingFlags.Public | BindingFlags.Instance);
            if (field is not null) return field.GetValue(target);

            throw new TemplateSyntaxException(
                $"Свойство '{name}' не найдено у объекта типа '{type.Name}' (путь '{fullPath}').");
        }
    }


    private abstract class Node;

    private sealed class TextNode(string text) : Node
    {
        public string Text { get; } = text;
    }

    private sealed class VariableNode(string path) : Node
    {
        public string Path { get; } = path;
    }

    private sealed class IfNode(string condition) : Node
    {
        public string Condition { get; } = condition;
        public List<Node> Then { get; } = new();
        public List<Node> Else { get; } = new();
    }

    private sealed class ForEachNode(string itemName, string collectionPath) : Node
    {
        public string ItemName { get; } = itemName;
        public string CollectionPath { get; } = collectionPath;
        public List<Node> Body { get; } = new();
    }

    
    private sealed class RenderContext
    {
        private readonly object _root;
        private readonly Stack<(string name, object? value)> _scopes = new();

        public RenderContext(object root)
        {
            _root = root ?? new object();
        }

        public object Root => _root;

        public void Push(string name, object? value) => _scopes.Push((name, value));

        public void Pop() => _scopes.Pop();

        public bool TryGetVariable(string name, out object? value)
        {
            foreach (var (n, v) in _scopes)
            {
                if (string.Equals(n, name, StringComparison.Ordinal))
                {
                    value = v;
                    return true;
                }
            }
            value = null;
            return false;
        }
    }

    private sealed class BlockFrame
    {
        public BlockFrame(Node node, List<Node> parent, bool isIf)
        {
            Node = node;
            Parent = parent;
            IsIf = isIf;
        }

        public List<Node> Parent { get; }
        public bool IsIf { get; }
        public IfNode? IfNode => Node as IfNode;
        public Node Node { get; }
    }
}

public sealed class TemplateSyntaxException : InvalidOperationException
{
    public TemplateSyntaxException(string message) : base(message) { }
}
