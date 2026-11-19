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
    public IHttpResult Profile()
    {
        var model = new
        {
            Title = "Профиль пользователя",

            User = new UserModel
            {
                Name = "Alex",
                Age = 20,
                Email = "alex@example.com",
                IsActive = true
            },

            Bio = "<script>alert('xss')</script> изучает C# и Web-разработку.",
            HasSubscription = true
        };

        return Page("User/Profile.html", model);
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

    public sealed class UserModel
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
