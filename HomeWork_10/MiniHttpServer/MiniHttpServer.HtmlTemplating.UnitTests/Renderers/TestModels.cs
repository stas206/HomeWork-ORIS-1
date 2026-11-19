namespace MiniHttpServer.HtmlTemplating.UnitTests.Renderers;

public sealed class UserModel
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public AddressModel? Address { get; set; }
    public List<string> Roles { get; set; } = new();
}

public sealed class AddressModel
{
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
}

public sealed class ProductModel
{
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool InStock { get; set; }
    public List<string> Tags { get; set; } = new();
}

public sealed class CatalogModel
{
    public string Title { get; set; } = string.Empty;
    public List<ProductModel> Products { get; set; } = new();
}
