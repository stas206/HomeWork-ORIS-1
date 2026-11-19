using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.ORM;

namespace MiniHttpServer.ORM.UnitTests;

[TestClass]
public class SqlCommandBuilderTests
{
    [TestMethod]
    public void BuildGetAll_ValidTable_ReturnsUnparameterizedSelect()
    {
        var command = SqlCommandBuilder.BuildGetAll("Users");

        Assert.AreEqual("SELECT * FROM \"Users\"", command.Text);
        Assert.AreEqual(0, command.Parameters.Count);
    }

    [TestMethod]
    public void BuildGetById_ValidTable_ReturnsParameterizedSelect()
    {
        var command = SqlCommandBuilder.BuildGetById("Users", 7);

        Assert.AreEqual("SELECT * FROM \"Users\" WHERE \"Id\" = @id", command.Text);
        Assert.AreEqual(1, command.Parameters.Count);
        Assert.AreEqual("@id", command.Parameters[0].Name);
        Assert.AreEqual(7, command.Parameters[0].Value);
    }

    [TestMethod]
    public void BuildInsert_User_ExcludesIdAndUsesParameters()
    {
        var user = new UserRow { Id = 0, Name = "Oleg", Email = "oleg@example.com", Age = 22 };

        var command = SqlCommandBuilder.BuildInsert("Users", user);

        Assert.AreEqual(
            "INSERT INTO \"Users\" (\"Name\", \"Email\", \"Age\") VALUES (@Name, @Email, @Age)",
            command.Text);

        Assert.IsFalse(command.Text.Contains("\"Id\"", StringComparison.Ordinal));
        Assert.IsFalse(command.Text.Contains("@Id", StringComparison.Ordinal));

        Assert.AreEqual(3, command.Parameters.Count);
        Assert.AreEqual("@Name", command.Parameters[0].Name);
        Assert.AreEqual("Oleg", command.Parameters[0].Value);
        Assert.AreEqual("@Email", command.Parameters[1].Name);
        Assert.AreEqual("oleg@example.com", command.Parameters[1].Value);
        Assert.AreEqual("@Age", command.Parameters[2].Name);
        Assert.AreEqual(22, command.Parameters[2].Value);
    }

    [TestMethod]
    public void BuildInsert_Product_GeneratesColumnsForDifferentModel()
    {
        var product = new ProductRow { Id = 0, Name = "Pen", Price = 15.5m };

        var command = SqlCommandBuilder.BuildInsert("Products", product);

        Assert.AreEqual(
            "INSERT INTO \"Products\" (\"Name\", \"Price\") VALUES (@Name, @Price)",
            command.Text);

        Assert.AreEqual(2, command.Parameters.Count);
        Assert.AreEqual("@Price", command.Parameters[1].Name);
        Assert.AreEqual(15.5m, command.Parameters[1].Value);
    }

    [TestMethod]
    public void BuildInsert_NullEntity_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => SqlCommandBuilder.BuildInsert<UserRow>("Users", null!));
    }

    [TestMethod]
    public void BuildInsert_InvalidTableName_Throws()
    {
        var user = new UserRow { Name = "A", Email = "a@b.c", Age = 1 };

        Assert.ThrowsException<ArgumentException>(
            () => SqlCommandBuilder.BuildInsert("bad name", user));
    }

    [TestMethod]
    public void BuildUpdate_User_ExcludesIdFromSetClause()
    {
        var user = new UserRow { Id = 0, Name = "Oleg", Email = "new@example.com", Age = 23 };

        var command = SqlCommandBuilder.BuildUpdate("Users", 1, user);

        Assert.AreEqual(
            "UPDATE \"Users\" SET \"Name\" = @Name, \"Email\" = @Email, \"Age\" = @Age WHERE \"Id\" = @id",
            command.Text);

        var setClause = command.Text.Split("WHERE")[0];
        Assert.IsFalse(setClause.Contains("\"Id\"", StringComparison.Ordinal));
        Assert.IsFalse(setClause.Contains("@Id", StringComparison.Ordinal));
        Assert.IsTrue(command.Text.EndsWith("WHERE \"Id\" = @id", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BuildUpdate_IncludesIdParameterLast()
    {
        var user = new UserRow { Name = "Oleg", Email = "x@example.com", Age = 23 };

        var command = SqlCommandBuilder.BuildUpdate("Users", 42, user);

        var last = command.Parameters[^1];
        Assert.AreEqual("@id", last.Name);
        Assert.AreEqual(42, last.Value);
        Assert.AreEqual(4, command.Parameters.Count);
    }

    [TestMethod]
    public void BuildUpdate_Product_GeneratesSetClauseForDifferentModel()
    {
        var product = new ProductRow { Id = 0, Name = "Pen", Price = 19.9m };

        var command = SqlCommandBuilder.BuildUpdate("Products", 3, product);

        Assert.AreEqual(
            "UPDATE \"Products\" SET \"Name\" = @Name, \"Price\" = @Price WHERE \"Id\" = @id",
            command.Text);
    }

    [TestMethod]
    public void BuildUpdate_NullEntity_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => SqlCommandBuilder.BuildUpdate<UserRow>("Users", 1, null!));
    }

    [TestMethod]
    public void BuildDelete_ValidTable_ReturnsParameterizedDelete()
    {
        var command = SqlCommandBuilder.BuildDelete("Users", 5);

        Assert.AreEqual("DELETE FROM \"Users\" WHERE \"Id\" = @id", command.Text);
        Assert.AreEqual(1, command.Parameters.Count);
        Assert.AreEqual("@id", command.Parameters[0].Name);
        Assert.AreEqual(5, command.Parameters[0].Value);
    }

    [TestMethod]
    public void BuildDelete_InvalidTableName_Throws()
    {
        Assert.ThrowsException<ArgumentException>(
            () => SqlCommandBuilder.BuildDelete("1bad", 5));
    }
}
