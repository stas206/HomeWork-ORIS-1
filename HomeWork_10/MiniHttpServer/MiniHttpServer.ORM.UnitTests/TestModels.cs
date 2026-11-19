namespace MiniHttpServer.ORM.UnitTests;

public sealed class UserRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}

public sealed class ProductRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public enum UserRole
{
    Guest = 0,
    Member = 1,
    Admin = 2
}

public sealed class AccountRow
{
    public int Id { get; set; }
    public UserRole Role { get; set; }
    public int? Rating { get; set; }
}
