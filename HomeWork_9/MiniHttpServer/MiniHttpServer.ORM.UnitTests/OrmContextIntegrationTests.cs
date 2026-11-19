using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.ORM;
using Npgsql;

namespace MiniHttpServer.ORM.UnitTests;

[TestClass]
public class OrmContextIntegrationTests
{
    private sealed class IntegrationItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Score { get; set; }
    }

    private static string? ConnectionString =>
        Environment.GetEnvironmentVariable("ORIS_DB_CONNECTION");

    private static bool IsConnectionAvailable()
    {
        var cs = ConnectionString;
        if (string.IsNullOrWhiteSpace(cs)) return false;

        try
        {
            using var connection = new NpgsqlConnection(cs);
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    [TestMethod]
    public async Task Crud_Cycle_WorksAgainstRealDatabase()
    {
        if (!IsConnectionAvailable())
        {
            Assert.Inconclusive(
                "Интеграционный тест требует PostgreSQL. Установите переменную окружения " +
                "ORIS_DB_CONNECTION со строкой подключения к базе orisdb.");
        }

        var orm = new OrmContext(ConnectionString!);
        var tableName = $"OrmTest_{Guid.NewGuid():N}";
        await CreateTableAsync(tableName);

        try
        {
            var inserted = await orm.InsertAsync(tableName,
                new IntegrationItem { Title = "First", Score = 10 });
            Assert.AreEqual(1, inserted);

            await orm.InsertAsync(tableName, new IntegrationItem { Title = "Second", Score = 20 });

            var all = await orm.GetAllAsync<IntegrationItem>(tableName);
            Assert.AreEqual(2, all.Count);

            var firstId = all.Single(i => i.Title == "First").Id;
            var byId = await orm.GetByIdAsync<IntegrationItem>(tableName, firstId);
            Assert.IsNotNull(byId);
            Assert.AreEqual(10, byId!.Score);

            var updated = await orm.UpdateAsync(tableName, firstId,
                new IntegrationItem { Title = "First", Score = 99 });
            Assert.AreEqual(1, updated);

            var afterUpdate = await orm.GetByIdAsync<IntegrationItem>(tableName, firstId);
            Assert.IsNotNull(afterUpdate);
            Assert.AreEqual(99, afterUpdate!.Score);

            var deleted = await orm.DeleteAsync(tableName, firstId);
            Assert.AreEqual(1, deleted);

            var remaining = await orm.GetAllAsync<IntegrationItem>(tableName);
            Assert.AreEqual(1, remaining.Count);
            Assert.AreEqual("Second", remaining[0].Title);
        }
        finally
        {
            await DropTableAsync(tableName);
        }
    }

    [TestMethod]
    public async Task Delete_NonExistingRow_ReturnsZero()
    {
        if (!IsConnectionAvailable())
        {
            Assert.Inconclusive("Интеграционный тест требует PostgreSQL (ORIS_DB_CONNECTION).");
        }

        var orm = new OrmContext(ConnectionString!);
        var tableName = $"OrmTest_{Guid.NewGuid():N}";
        await CreateTableAsync(tableName);

        try
        {
            var deleted = await orm.DeleteAsync(tableName, 999_999);
            Assert.AreEqual(0, deleted);
        }
        finally
        {
            await DropTableAsync(tableName);
        }
    }

    private static async Task CreateTableAsync(string tableName)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            $"CREATE TABLE \"{tableName}\" (" +
            "\"Id\" SERIAL PRIMARY KEY, " +
            "\"Title\" VARCHAR(100) NOT NULL, " +
            "\"Score\" INTEGER NOT NULL)", connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DropTableAsync(string tableName)
    {
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"DROP TABLE \"{tableName}\"", connection);
            await command.ExecuteNonQueryAsync();
        }
        catch
        {
        }
    }
}
