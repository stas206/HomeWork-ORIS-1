using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.ORM;

namespace MiniHttpServer.ORM.UnitTests;

[TestClass]
public class EntityMapperTests
{
    private static readonly string[] UserColumns = { "Id", "Name", "Email", "Age" };

    [TestMethod]
    public void MapRow_AllColumnsPresent_MapsEveryProperty()
    {
        var reader = FakeRecordReader.Single(UserColumns, new object?[] { 1, "Alex", "alex@example.com", 20 });
        reader.Read();

        var entity = EntityMapper.MapRow<UserRow>(reader);

        Assert.AreEqual(1, entity.Id);
        Assert.AreEqual("Alex", entity.Name);
        Assert.AreEqual("alex@example.com", entity.Email);
        Assert.AreEqual(20, entity.Age);
    }

    [TestMethod]
    public void MapRow_CaseInsensitiveColumnNames_MapsCorrectly()
    {
        var reader = FakeRecordReader.Single(new[] { "id", "NAME", "EMAIL", "age" },
            new object?[] { 2, "Maria", "maria@example.com", 21 });
        reader.Read();

        var entity = EntityMapper.MapRow<UserRow>(reader);

        Assert.AreEqual(2, entity.Id);
        Assert.AreEqual("Maria", entity.Name);
        Assert.AreEqual(21, entity.Age);
    }

    [TestMethod]
    public void MapRow_MissingColumns_LeavePropertyDefaults()
    {
        var reader = FakeRecordReader.Single(new[] { "Name", "Age" },
            new object?[] { "Ivan", 30 });
        reader.Read();

        var entity = EntityMapper.MapRow<UserRow>(reader);

        Assert.AreEqual(0, entity.Id);
        Assert.AreEqual("Ivan", entity.Name);
        Assert.AreEqual(string.Empty, entity.Email);
        Assert.AreEqual(30, entity.Age);
    }

    [TestMethod]
    public void MapRow_NullOnReferenceType_SetsNull()
    {
        var reader = FakeRecordReader.Single(UserColumns,
            new object?[] { 1, null, "e@example.com", 20 });
        reader.Read();

        var entity = EntityMapper.MapRow<UserRow>(reader);

        Assert.IsNull(entity.Name);
        Assert.AreEqual("e@example.com", entity.Email);
    }

    [TestMethod]
    public void MapRow_NullOnNonNullableValueType_KeepsDefault()
    {
        var reader = FakeRecordReader.Single(UserColumns,
            new object?[] { 1, "Alex", "e@example.com", DBNull.Value });
        reader.Read();

        var entity = EntityMapper.MapRow<UserRow>(reader);

        Assert.AreEqual(0, entity.Age);
    }

    [TestMethod]
    public void MapRow_EnumConvertedFromInt()
    {
        var reader = FakeRecordReader.Single(new[] { "Id", "Role", "Rating" },
            new object?[] { 1, 2, null });
        reader.Read();

        var entity = EntityMapper.MapRow<AccountRow>(reader);

        Assert.AreEqual(UserRole.Admin, entity.Role);
        Assert.IsNull(entity.Rating);
    }

    [TestMethod]
    public void MapRow_NullableValueTypeWithValue_MapsValue()
    {
        var reader = FakeRecordReader.Single(new[] { "Id", "Role", "Rating" },
            new object?[] { 1, 1, 5 });
        reader.Read();

        var entity = EntityMapper.MapRow<AccountRow>(reader);

        Assert.AreEqual(UserRole.Member, entity.Role);
        Assert.AreEqual(5, entity.Rating);
    }

    [TestMethod]
    public void MapAll_MultipleRows_MapsAllEntities()
    {
        var reader = new FakeRecordReader(UserColumns,
            new object?[] { 1, "Alex", "a@example.com", 20 },
            new object?[] { 2, "Maria", "m@example.com", 21 },
            new object?[] { 3, "Ivan", "i@example.com", 19 });

        var entities = EntityMapper.MapAll<UserRow>(reader);

        Assert.AreEqual(3, entities.Count);
        Assert.AreEqual("Alex", entities[0].Name);
        Assert.AreEqual("Maria", entities[1].Name);
        Assert.AreEqual("Ivan", entities[2].Name);
    }

    [TestMethod]
    public void MapAll_EmptyReader_ReturnsEmptyList()
    {
        var reader = FakeRecordReader.Empty(UserColumns);

        var entities = EntityMapper.MapAll<UserRow>(reader);

        Assert.AreEqual(0, entities.Count);
    }

    [TestMethod]
    public void MapSingle_WithRow_ReturnsEntity()
    {
        var reader = FakeRecordReader.Single(UserColumns,
            new object?[] { 7, "Oleg", "oleg@example.com", 22 });

        var entity = EntityMapper.MapSingle<UserRow>(reader);

        Assert.IsNotNull(entity);
        Assert.AreEqual(7, entity!.Id);
        Assert.AreEqual("Oleg", entity.Name);
    }

    [TestMethod]
    public void MapSingle_WithoutRows_ReturnsNull()
    {
        var reader = FakeRecordReader.Empty(UserColumns);

        var entity = EntityMapper.MapSingle<UserRow>(reader);

        Assert.IsNull(entity);
    }

    [TestMethod]
    public void MapAll_WorksWithDifferentModel()
    {
        var reader = new FakeRecordReader(new[] { "Id", "Name", "Price" },
            new object?[] { 1, "Pen", 15.5m },
            new object?[] { 2, "Notebook", 120m });

        var products = EntityMapper.MapAll<ProductRow>(reader);

        Assert.AreEqual(2, products.Count);
        Assert.AreEqual("Pen", products[0].Name);
        Assert.AreEqual(15.5m, products[0].Price);
        Assert.AreEqual("Notebook", products[1].Name);
    }
}
