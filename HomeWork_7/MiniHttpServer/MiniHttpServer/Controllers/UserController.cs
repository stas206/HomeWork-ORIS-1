using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;

namespace MiniHttpServer.Controllers;

[Controller]
public sealed class UserController : BaseController
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
    public IHttpResult List()
    {
        var model = new
        {
            Title = "Список пользователей",
            Users = new List<UserModel>
            {
                new() { Name = "Stas",   Age = 20, Email = "stas@example.com",   IsActive = true },
                new() { Name = "Maria",  Age = 25, Email = "maria@example.com",  IsActive = false },
                new() { Name = "Oleg",   Age = 30, Email = "oleg@example.com",   IsActive = true },
                new() { Name = "Aigul",  Age = 22, Email = "aigul@example.com",  IsActive = false }
            }
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
