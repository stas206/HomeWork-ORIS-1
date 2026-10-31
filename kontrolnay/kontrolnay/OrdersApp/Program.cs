using System.Net;
using System.Text;
using OrdersApp;

var listener = new HttpListener();
listener.Prefixes.Add("http://localhost:8080/");
listener.Start();
Console.WriteLine("http://localhost:8080/orders");

while (true)
{
    var ctx = listener.GetContext();
    try
    {
        Handle(ctx);
    }
    catch (Exception ex)
    {
        Send(ctx, 500, "<h1>500</h1><p>" + WebUtility.HtmlEncode(ex.Message) + "</p>");
    }
}

static void Handle(HttpListenerContext ctx)
{
    var req = ctx.Request;
    var path = req.Url.AbsolutePath.TrimEnd('/');
    if (path == "") path = "/";

    if (req.HttpMethod == "GET" && path == "/orders")
    {
        int? min = null;
        var raw = req.QueryString["minAmount"];
        if (raw != null)
        {
            if (!int.TryParse(raw, out var m) || m < 0)
            {
                Send(ctx, 400, "<h1>400</h1><p>minAmount должен быть неотрицательным числом</p>");
                return;
            }
            min = m;
        }

        var list = Store.Filter(min);

        var sb = new StringBuilder();
        sb.Append("<h1>Заказы</h1>");
        sb.Append("<form method='get'>Минимальная сумма: <input name='minAmount' type='number' min='0' value='" + (min?.ToString() ?? "") + "'> <button>Фильтр</button> <a href='/orders'>Сброс</a></form>");
        sb.Append("<p><a href='/orders/create'>Создать заказ</a></p>");
        sb.Append("<table border='1' cellpadding='4'><tr><th>Id</th><th>Покупатель</th><th>Сумма</th><th>Статус</th><th></th></tr>");
        foreach (var o in list)
        {
            sb.Append("<tr>");
            sb.Append("<td>" + o.Id + "</td>");
            sb.Append("<td>" + WebUtility.HtmlEncode(o.Customer) + "</td>");
            sb.Append("<td>" + o.Amount + "</td>");
            sb.Append("<td>" + o.Status + "</td>");
            sb.Append("<td><a href='/orders/details?id=" + o.Id + "'>детали</a></td>");
            sb.Append("</tr>");
        }
        sb.Append("</table>");
        sb.Append("<p><b>Найдено заказов:</b> " + list.Count + "</p>");
        Send(ctx, 200, Page("Заказы", sb.ToString()));
        return;
    }

    if (req.HttpMethod == "GET" && path == "/orders/details")
    {
        if (!int.TryParse(req.QueryString["id"], out var id))
        {
            Send(ctx, 400, "<h1>400</h1><p>Нужен параметр id</p>");
            return;
        }
        var o = Store.Find(id);
        if (o == null)
        {
            Send(ctx, 404, "<h1>404</h1><p>Заказ не найден</p>");
            return;
        }

        var body = $@"
            <h1>Заказ #{o.Id}</h1>
            <p>Покупатель: {WebUtility.HtmlEncode(o.Customer)}</p>
            <p>Сумма: {o.Amount}</p>
            <p>Статус: {o.Status}</p>

            <h2>Редактировать</h2>
            <form id='f'>
              Покупатель: <input name='customer' value='{WebUtility.HtmlEncode(o.Customer)}'><br>
              Сумма: <input name='amount' type='number' min='1' max='100000' value='{o.Amount}'><br>
              Статус: <select name='status'>
                <option value='unpaid' {(o.Status == "unpaid" ? "selected" : "")}>unpaid</option>
                <option value='paid' {(o.Status == "paid" ? "selected" : "")}>paid</option>
              </select><br>
              <button>Сохранить</button>
            </form>
            <pre id='r'></pre>
            <script>
              document.getElementById('f').addEventListener('submit', async e => {{
                e.preventDefault();
                const data = new URLSearchParams(new FormData(e.target));
                const resp = await fetch('/orders/update?id={o.Id}', {{
                  method: 'PUT',
                  headers: {{'Content-Type':'application/x-www-form-urlencoded'}},
                  body: data
                }});
                document.getElementById('r').textContent =
                  resp.status === 204 ? 'Сохранено' : ('Ошибка ' + resp.status);
              }});
            </script>
            <p><a href='/orders'>Назад</a></p>";

        Send(ctx, 200, Page("Заказ #" + o.Id, body));
        return;
    }

    if (req.HttpMethod == "GET" && path == "/orders/create")
    {
        var body = @"
            <h1>Новый заказ</h1>
            <form method='post' action='/orders/create'>
              Покупатель (2..60): <input name='customer'><br>
              Сумма (1..100000): <input name='amount' type='number' min='1' max='100000'><br>
              <button>Создать</button>
            </form>
            <p>Статус при создании — unpaid.</p>
            <p><a href='/orders'>Назад</a></p>";
        Send(ctx, 200, Page("Новый заказ", body));
        return;
    }

    if (req.HttpMethod == "POST" && path == "/orders/create")
    {
        var form = ReadForm(req);
        var customer = form.GetValueOrDefault("customer");
        var amountStr = form.GetValueOrDefault("amount");

        if (!Store.CustomerOk(customer) ||
            !int.TryParse(amountStr, out var amount) ||
            !Store.AmountOk(amount))
        {
            Send(ctx, 400, "<h1>400</h1><p>Неверные данные</p><p><a href='/orders/create'>Назад</a></p>");
            return;
        }

        var created = Store.Create(customer, amount);

        ctx.Response.StatusCode = 303;
        ctx.Response.RedirectLocation = "/orders/details?id=" + created.Id;
        ctx.Response.Close();
        return;
    }

    if (req.HttpMethod == "PUT" && path == "/orders/update")
    {
        if (!int.TryParse(req.QueryString["id"], out var id))
        {
            Send(ctx, 400, "<h1>400</h1><p>Нужен id</p>");
            return;
        }
        if (Store.Find(id) == null)
        {
            Send(ctx, 404, "<h1>404</h1><p>Заказ не найден</p>");
            return;
        }

        var form = ReadForm(req);
        var customer = form.GetValueOrDefault("customer");
        var amountStr = form.GetValueOrDefault("amount");
        var status = form.GetValueOrDefault("status");

        if (!Store.CustomerOk(customer) ||
            !int.TryParse(amountStr, out var amount) ||
            !Store.AmountOk(amount) ||
            !Store.StatusOk(status))
        {
            Send(ctx, 400, "<h1>400</h1><p>Нужны все поля: customer, amount, status (unpaid|paid)</p>");
            return;
        }

        Store.Update(id, customer, amount, status);

        ctx.Response.StatusCode = 204;
        ctx.Response.Close();
        return;
    }

    Send(ctx, 404, "<h1>404</h1><p>Нет такого маршрута</p>");
}

static Dictionary<string, string> ReadForm(HttpListenerRequest req)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    if (!req.HasEntityBody) return result;

    using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
    var body = reader.ReadToEnd();

    foreach (var pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
    {
        var i = pair.IndexOf('=');
        string k = i < 0 ? pair : pair.Substring(0, i);
        string v = i < 0 ? "" : pair.Substring(i + 1);
        k = Uri.UnescapeDataString(k.Replace('+', ' '));
        v = Uri.UnescapeDataString(v.Replace('+', ' '));
        result[k] = v;
    }
    return result;
}

static string Page(string title, string body) => $@"
    <!doctype html>
    <html><head><meta charset='utf-8'><title>{WebUtility.HtmlEncode(title)}</title></head>
    <body style='font-family:sans-serif;margin:2em'>
        {body}
    </body></html>";

static void Send(HttpListenerContext ctx, int code, string html)
{
    var bytes = Encoding.UTF8.GetBytes(html);
    ctx.Response.StatusCode = code;
    ctx.Response.ContentType = "text/html; charset=utf-8";
    ctx.Response.ContentLength64 = bytes.Length;
    ctx.Response.OutputStream.Write(bytes);
    ctx.Response.Close();
}