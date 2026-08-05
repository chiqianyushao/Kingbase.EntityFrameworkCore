using Kdbndp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AbpBookStore.EntityFrameworkCore.Tests.Capability;

/// <summary>
/// Re-verifies the report's §13 (Migration operations) and §14 (design-time /
/// toolchain) end-to-end: apply the sample's InitialCreate migration to a
/// throwaway database, confirm the history table + applied/pending state, and
/// generate the idempotent script — all through the ABP BookStore DbContext
/// wired to the Kingbase provider.
///
/// Requires KINGBASE_ADMIN_CONNECTION so a throwaway database can be created and
/// dropped (the shared test database is managed by EnsureCreated and must not
/// receive the migration's schema). Skips silently when the admin connection is
/// not configured.
/// </summary>
public sealed class CapabilityMigrationsTests : CapabilityTestBase
{
    public const string AdminConnectionVariable = TestDatabase.AdminConnectionVariable;
    public const string ThrowawayDatabaseName = "abp_bookstore_migration_test";

    public CapabilityMigrationsTests(AbpAppFixture fixture, ITestOutputHelper output)
        : base(fixture, output)
    {
    }

    [Fact]
    public async Task Initial_create_migration_applies_history_scripts_and_rolls_back()
    {
        var adminConnection = Environment.GetEnvironmentVariable(AdminConnectionVariable);
        if (string.IsNullOrWhiteSpace(adminConnection))
        {
            return; // no admin connection -> silently skip (offline green)
        }

        var throwawayConnection = await CreateThrowawayDatabaseAsync(adminConnection);
        try
        {
            var options = new DbContextOptionsBuilder<BookStoreDbContext>()
                .UseKdbndp(throwawayConnection, kingbase => kingbase.SetOracleCompatibilityMode())
                .Options;

            await using (var context = new BookStoreDbContext(options))
            {
                // Migration ids are "<timestamp>_InitialCreate", so match by predicate.
                var pending = await context.Database.GetPendingMigrationsAsync();
                Assert.Contains(pending, m => m.EndsWith("InitialCreate"));
                Assert.Empty(await context.Database.GetAppliedMigrationsAsync());

                await context.Database.MigrateAsync();

                var applied = await context.Database.GetAppliedMigrationsAsync();
                Assert.Contains(applied, m => m.EndsWith("InitialCreate"));
                Assert.Empty(await context.Database.GetPendingMigrationsAsync());

                // History table exists with the applied row.
                var historyCount = await context.Database
                    .SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM \"__EFMigrationsHistory\"")
                    .SingleAsync();
                Assert.Equal(1L, historyCount);

                // Schema matches the model (tables created by the migration).
                var tables = await context.Database
                    .SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM information_schema.tables WHERE table_schema = current_schema() AND table_name = 'Books'")
                    .SingleAsync();
                Assert.Equal(1L, tables);
            }

            // Idempotent script generation (design-time, no DB writes).
            await using (var context = new BookStoreDbContext(options))
            {
                var script = context.GetService<IMigrator>()
                    .GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
                Assert.Contains("DO $EF$", script);
                Assert.Contains("__EFMigrationsHistory", script);
                Assert.Contains("InitialCreate", script);
            }

            // Roll back to the initial (empty) database — drops all tables + history.
            await using (var context = new BookStoreDbContext(options))
            {
                await context.Database.MigrateAsync();
                await context.GetService<IMigrator>().MigrateAsync(Migration.InitialDatabase);
                Assert.Empty(await context.Database.GetAppliedMigrationsAsync());
            }
        }
        finally
        {
            // Close every pooled connection to the throwaway database first —
            // Kdbndp's connection pool keeps idle connections open, and DROP
            // DATABASE fails with 55006 ("database is being accessed by other
            // users") while any pool connection is still alive.
            await using (var poolClearer = new KdbndpConnection(throwawayConnection))
            {
                KdbndpConnection.ClearPool(poolClearer);
            }
            await DropThrowawayDatabaseAsync(adminConnection);
        }
    }

    private static async Task<string> CreateThrowawayDatabaseAsync(string adminConnection)
    {
        await using var connection = new KdbndpConnection(adminConnection);
        await connection.OpenAsync();

        await using var existsCommand = connection.CreateCommand();
        existsCommand.CommandText = "SELECT COUNT(*) FROM sys_database WHERE datname = @name";
        var nameParameter = existsCommand.CreateParameter();
        nameParameter.ParameterName = "name";
        nameParameter.Value = ThrowawayDatabaseName;
        existsCommand.Parameters.Add(nameParameter);
        var exists = Convert.ToInt64(await existsCommand.ExecuteScalarAsync()) > 0;
        if (!exists)
        {
            await using var createCommand = connection.CreateCommand();
            createCommand.CommandText = $"CREATE DATABASE \"{ThrowawayDatabaseName}\"";
            await createCommand.ExecuteNonQueryAsync();
        }

        var builder = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = adminConnection };
        builder["Database"] = ThrowawayDatabaseName;
        return builder.ConnectionString;
    }

    private static async Task DropThrowawayDatabaseAsync(string adminConnection)
    {
        await using var connection = new KdbndpConnection(adminConnection);
        await connection.OpenAsync();

        // Drop the throwaway DB. Its connections must all be closed first.
        await using var dropCommand = connection.CreateCommand();
        dropCommand.CommandText = $"DROP DATABASE IF EXISTS \"{ThrowawayDatabaseName}\"";
        await dropCommand.ExecuteNonQueryAsync();
    }
}
