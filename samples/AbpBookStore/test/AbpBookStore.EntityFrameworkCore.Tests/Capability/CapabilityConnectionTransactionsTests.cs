using AbpBookStore.Books;
using Kdbndp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace AbpBookStore.EntityFrameworkCore.Tests.Capability;

/// <summary>
/// Re-verifies the report's §11 (connection, transaction, raw SQL, execution
/// strategy) and §12 (RelationalDatabaseFacadeExtensions) on a real KingbaseES
/// database through the ABP BookStore DbContext + Kingbase provider.
///
/// Transaction-boundary tests (BeginTransaction, savepoints, UseTransaction) run
/// through the ABP-resolved DbContext inside a NON-transactional unit of work:
/// ABP's AbpUnitOfWorkOptions.IsTransactional defaults to false, so an explicit
/// BeginTransaction on the context does not collide with an ABP-managed
/// transaction. UseTransaction additionally needs the context to own the
/// external connection, so that context is built directly on the Kdbndp
/// connection and given a LazyServiceProvider manually (the same service the
/// Autofac property-injection would set) so AbpDbContext.SaveChangesAsync works.
/// </summary>
public sealed class CapabilityConnectionTransactionsTests : CapabilityTestBase
{
    public CapabilityConnectionTransactionsTests(AbpAppFixture fixture, ITestOutputHelper output)
        : base(fixture, output)
    {
    }

    [Fact]
    public async Task Begin_transaction_commit_and_rollback_execute()
    {
        if (!HasDatabase) return;
        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);

        using var scope = CreateScope();
        using var uow = BeginUnitOfWork(scope);   // non-transactional by default
        var context = await GetDbContextAsync(scope);

        // Commit path (async)
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            context.Books.Add(NewBook("Txn Commit", BookType.Adventure, new DateTime(2001, 1, 1), 1));
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        Assert.Equal(1, await context.Books.CountAsync());

        // Rollback path (sync)
        using (var transaction = context.Database.BeginTransaction())
        {
            context.Books.Add(NewBook("Txn Rollback", BookType.Biography, new DateTime(2002, 1, 1), 2));
            await context.SaveChangesAsync();
            transaction.Rollback();
        }
        Assert.Equal(1, await context.Books.CountAsync()); // rolled back row gone
        Assert.Equal(1, await context.Books.CountAsync(b => b.Name == "Txn Commit"));
        Assert.Equal(0, await context.Books.CountAsync(b => b.Name == "Txn Rollback"));
    }

    [Fact]
    public async Task Savepoints_create_rollback_to_and_commit()
    {
        if (!HasDatabase) return;
        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);

        using var scope = CreateScope();
        using var uow = BeginUnitOfWork(scope);
        var context = await GetDbContextAsync(scope);

        await using var transaction = await context.Database.BeginTransactionAsync();

        context.Books.Add(NewBook("Savepoint Kept", BookType.Adventure, new DateTime(2001, 1, 1), 1));
        await context.SaveChangesAsync();
        await transaction.CreateSavepointAsync("before-batch");

        context.Books.Add(NewBook("Savepoint Rolled Back", BookType.Biography, new DateTime(2002, 1, 1), 2));
        await context.SaveChangesAsync();
        await transaction.RollbackToSavepointAsync("before-batch");

        await transaction.CommitAsync();

        Assert.Equal(1, await context.Books.CountAsync(b => b.Name == "Savepoint Kept"));
        Assert.Equal(0, await context.Books.CountAsync(b => b.Name == "Savepoint Rolled Back"));
    }

    [Fact]
    public async Task Use_transaction_with_external_kdbndp_connection()
    {
        if (!HasDatabase) return;
        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);

        // External Kdbndp connection + transaction. The context must own THIS
        // connection for UseTransaction to accept the transaction, so it is
        // built directly on it and given a LazyServiceProvider manually.
        await using var connection = new KdbndpConnection(Fixture.ConnectionString!);
        await connection.OpenAsync();
        await using var externalTransaction = await connection.BeginTransactionAsync();

        using var scope = CreateScope();
        var context = new BookStoreDbContext(
            new DbContextOptionsBuilder<BookStoreDbContext>()
                .UseKdbndp(connection, contextOwnsConnection: false,
                    kingbase => kingbase.SetOracleCompatibilityMode())
                .Options);
        context.LazyServiceProvider = new AbpLazyServiceProvider(scope.ServiceProvider);

        await context.Database.UseTransactionAsync(externalTransaction);

        context.Books.Add(NewBook("External Txn", BookType.Adventure, new DateTime(2001, 1, 1), 1));
        await context.SaveChangesAsync();
        await externalTransaction.CommitAsync();

        Assert.Equal(1, await context.Books.CountAsync(b => b.Name == "External Txn"));
    }

    [Fact]
    public async Task Raw_sql_execute_sql_and_scalar_queries()
    {
        if (!HasDatabase) return;
        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);
        await SeedBooksAsync(NewBook("Raw SQL", BookType.Adventure, new DateTime(2001, 1, 1), 5));

        await InDbContextAsync(async context =>
        {
            // ExecuteSqlRaw / ExecuteSql / ExecuteSqlInterpolated
            Assert.Equal(1, await context.Database.ExecuteSqlRawAsync(
                "UPDATE \"Books\" SET \"Price\" = {0} WHERE \"Name\" = 'Raw SQL'", 42));
            Assert.Equal(1, await context.Database.ExecuteSqlAsync(
                $"UPDATE \"Books\" SET \"Price\" = {43} WHERE \"Name\" = 'Raw SQL'"));
            Assert.Equal(1, context.Database.ExecuteSqlInterpolated(
                $"UPDATE \"Books\" SET \"Price\" = {44} WHERE \"Name\" = 'Raw SQL'"));

            Assert.Equal(44, (await context.Books.SingleAsync()).Price);

            // SqlQuery / SqlQueryRaw (scalar; EF requires the column aliased "Value")
            var scalar = await context.Database
                .SqlQuery<int>($"SELECT CAST(\"Price\" AS integer) AS \"Value\" FROM \"Books\" WHERE \"Name\" = 'Raw SQL'")
                .SingleAsync();
            Assert.Equal(44, scalar);

            var scalarRaw = await context.Database
                .SqlQueryRaw<int>("SELECT CAST(\"Price\" AS integer) AS \"Value\" FROM \"Books\" WHERE \"Name\" = 'Raw SQL'")
                .SingleAsync();
            Assert.Equal(44, scalarRaw);
            return 0;
        });
    }

    [Fact]
    public async Task Connection_lifecycle_and_db_facade_extensions()
    {
        if (!HasDatabase) return;
        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);

        await InDbContextAsync(async context =>
        {
            var facade = context.Database;

            // GetDbConnection returns a native KdbndpConnection
            var connection = facade.GetDbConnection();
            Assert.IsType<KdbndpConnection>(connection);
            Assert.False(connection.State == System.Data.ConnectionState.Open);

            // Open/Close lifecycle
            await facade.OpenConnectionAsync();
            Assert.Equal(System.Data.ConnectionState.Open, connection.State);
            await facade.CloseConnectionAsync();
            Assert.Equal(System.Data.ConnectionState.Closed, connection.State);

            // IsRelational / GetConnectionString / GetCommandTimeout
            Assert.True(facade.IsRelational());
            Assert.Contains("abp_bookstore", facade.GetConnectionString());

            facade.SetCommandTimeout(90);
            var timeout = facade.GetCommandTimeout();
            Assert.True(timeout.HasValue);
            Assert.Equal(90, timeout!.Value);

            // GenerateCreateScript
            var script = facade.GenerateCreateScript();
            Assert.Contains("CREATE TABLE", script);
            Assert.Contains("\"Books\"", script);

            // Execution strategy is wired through (provider enables retry by default)
            var strategy = facade.CreateExecutionStrategy();
            Assert.NotNull(strategy);
            return 0;
        });
    }

    [Fact]
    public async Task Execution_strategy_executes_through_di()
    {
        if (!HasDatabase) return;
        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);

        using var scope = CreateScope();
        using var uow = BeginUnitOfWork(scope);
        var context = await GetDbContextAsync(scope);

        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            Assert.Equal(0, await context.Books.CountAsync());
        });

        await uow.CompleteAsync();
    }
}
