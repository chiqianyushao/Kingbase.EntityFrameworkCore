using System.Data.Common;
using AbpBookStore.EntityFrameworkCore;
using Kdbndp;
using Microsoft.EntityFrameworkCore;

namespace AbpBookStore.TestBase;

/// <summary>
/// Connection resolution and schema reset helpers for the real-Kingbase
/// integration tests. The tests skip silently when neither connection
/// environment variable is present (same convention as the provider tests).
///
/// Resolution order:
///   1. KINGBASE_TEST_CONNECTION   -> used as-is (must point to a dedicated
///                                    test database, e.g. abp_bookstore_dev)
///   2. KINGBASE_ADMIN_CONNECTION  -> creates database abp_bookstore_dev if
///                                    missing, then targets it
/// </summary>
public static class TestDatabase
{
    public const string TestConnectionVariable = "KINGBASE_TEST_CONNECTION";
    public const string AdminConnectionVariable = "KINGBASE_ADMIN_CONNECTION";
    public const string TargetDatabaseName = "abp_bookstore_dev";

    /// <summary>Returns the connection string to use, or null when no database is configured.</summary>
    public static async Task<string?> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var testConnection = Environment.GetEnvironmentVariable(TestConnectionVariable);
        if (!string.IsNullOrWhiteSpace(testConnection))
        {
            return testConnection;
        }

        var adminConnection = Environment.GetEnvironmentVariable(AdminConnectionVariable);
        if (string.IsNullOrWhiteSpace(adminConnection))
        {
            return null;
        }

        await EnsureDatabaseExistsAsync(adminConnection, TargetDatabaseName, cancellationToken);

        var builder = new DbConnectionStringBuilder { ConnectionString = adminConnection };
        builder["Database"] = TargetDatabaseName;
        return builder.ConnectionString;
    }

    /// <summary>
    /// Drops the three ABP BookStore tables and recreates the schema from the
    /// model. Safe to call when the target database also contains unrelated
    /// tables: EnsureCreated only creates when the database is empty, so a
    /// GenerateCreateScript() fallback is executed when it reports false.
    ///
    /// Uses a directly-constructed BookStoreDbContext because only DDL runs
    /// here (drop / EnsureCreated / script) — none of it goes through
    /// SaveChanges, so the ABP LazyServiceProvider-dependent audit path is
    /// never touched.
    /// </summary>
    public static async Task ResetSchemaAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        // The remote Kingbase server is slow/flaky for the first connection of a
        // process; retry until the pool is warm before issuing any DDL.
        await OpenWithRetryAsync(connectionString, cancellationToken);

        var options = new DbContextOptionsBuilder<BookStoreDbContext>()
            .UseKdbndp(connectionString, kingbase => kingbase.SetOracleCompatibilityMode())
            .Options;

        await using var context = new BookStoreDbContext(options);

        await context.Database.ExecuteSqlRawAsync(
            "DROP TABLE IF EXISTS \"BookAuthors\" CASCADE", cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "DROP TABLE IF EXISTS \"Books\" CASCADE", cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "DROP TABLE IF EXISTS \"Authors\" CASCADE", cancellationToken);

        if (await context.Database.EnsureCreatedAsync(cancellationToken))
        {
            return;
        }

        await context.Database.ExecuteSqlRawAsync(
            context.Database.GenerateCreateScript(), cancellationToken);
    }

    /// <summary>
    /// Opens (and immediately closes) a raw connection with backoff retries, to
    /// warm the pool through the flaky first-connection phase of the remote
    /// server. Returns once a connection succeeds.
    /// </summary>
    private static async Task OpenWithRetryAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            try
            {
                await using var connection = new KdbndpConnection(connectionString);
                await connection.OpenAsync(cancellationToken);
                return;
            }
            catch (Exception exception) when (attempt < 4)
            {
                lastException = exception;
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "Unable to connect to the KingbaseES database after four attempts.",
            lastException);
    }

    private static async Task EnsureDatabaseExistsAsync(
        string adminConnection,
        string databaseName,
        CancellationToken cancellationToken)
    {
        await OpenWithRetryAsync(adminConnection, cancellationToken);
        await using var connection = new KdbndpConnection(adminConnection);
        await connection.OpenAsync(cancellationToken);

        await using var existsCommand = connection.CreateCommand();
        existsCommand.CommandText = "SELECT COUNT(*) FROM sys_database WHERE datname = @name";
        var nameParameter = existsCommand.CreateParameter();
        nameParameter.ParameterName = "name";
        nameParameter.Value = databaseName;
        existsCommand.Parameters.Add(nameParameter);

        var exists = Convert.ToInt64(await existsCommand.ExecuteScalarAsync(cancellationToken)) > 0;
        if (exists)
        {
            return;
        }

        await using var createCommand = connection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        await createCommand.ExecuteNonQueryAsync(cancellationToken);
    }
}
