using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.Models;
using MiniHttpServer.ORM;

namespace MiniHttpServer.Controllers;

[Controller]
public sealed class UserController(IOrmContext orm) : BaseController
{
    [Get]
    [Authorize]
    public IHttpResult Profile()
    {
        return Html("<h1>Личный кабинет</h1>");
    }

    [Get]
    public async Task<IHttpResult> List()
    {
        var users = await orm.GetAllAsync<User>("Users");

        var model = new
        {
            Title = "Список пользователей",
            Users = users
        };

        return Page("User/List.html", model);
    }
}
