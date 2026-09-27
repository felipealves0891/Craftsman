using System.Data.Common;
using Craftsman.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.App.E2E;

public sealed class E2EDatabaseResetter
{
    private readonly AppDbContext dbContext;
    private readonly IWebHostEnvironment environment;

    public E2EDatabaseResetter(AppDbContext dbContext, IWebHostEnvironment environment)
    {
        this.dbContext = dbContext;
        this.environment = environment;
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        EnsureE2EEnvironment();

        await dbContext.Database.MigrateAsync(cancellationToken);

        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var tableNames = await ListResettableTablesAsync(connection, cancellationToken);
        if (tableNames.Count == 0)
        {
            return;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"TRUNCATE TABLE {string.Join(", ", tableNames.Select(QuotePublicTable))} RESTART IDENTITY CASCADE;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private void EnsureE2EEnvironment()
    {
        if (!environment.IsEnvironment("E2E"))
        {
            throw new InvalidOperationException("E2E database reset can only run in the E2E environment.");
        }
    }

    private static async Task<IReadOnlyCollection<string>> ListResettableTablesAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        var tableNames = new List<string>();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            select tablename
            from pg_tables
            where schemaname = 'public'
              and tablename <> '__EFMigrationsHistory'
            order by tablename;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tableNames.Add(reader.GetString(0));
        }

        return tableNames;
    }

    private static string QuotePublicTable(string tableName)
        => $"public.{QuoteIdentifier(tableName)}";

    private static string QuoteIdentifier(string identifier)
        => $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
