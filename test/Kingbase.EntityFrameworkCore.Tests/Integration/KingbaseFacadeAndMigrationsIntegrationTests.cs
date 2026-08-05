using System.Data;
using System.Data.Common;
using System.Transactions;
using Kdbndp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kingbase.EntityFrameworkCore.Tests.Integration;

public sealed class KingbaseFacadeAndMigrationsIntegrationTests
{
    private const string ConnectionVariable = "KINGBASE_TEST_CONNECTION";

    [Fact]
    public async Task Facade_transaction_raw_sql_timeout_compiled_query_and_interceptors_work()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var interceptor = new CountingCommandInterceptor();
        var options = new DbContextOptionsBuilder<FacadeDbContext>()
            .UseKdbndp(connectionString, options => options.SetOracleCompatibilityMode().EnableRetryOnFailure())
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new FacadeDbContext(options);
        await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS \"efcore_facade_probe\"");
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE \"efcore_facade_probe\" (\"Id\" integer PRIMARY KEY, \"Name\" text NOT NULL)");

        context.Database.SetCommandTimeout(TimeSpan.FromSeconds(17));
        Assert.Equal(17, context.Database.GetCommandTimeout());
        Assert.False(string.IsNullOrWhiteSpace(context.Database.GetConnectionString()));
        Assert.IsType<KdbndpConnection>(context.Database.GetDbConnection());
        Assert.True(context.Database.IsRelational());
        Assert.True(context.Database.CreateExecutionStrategy().RetriesOnFailure);
        var retryAttempts = 0;
        Assert.Equal(9, await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            if (Interlocked.Increment(ref retryAttempts) == 1) throw new TimeoutException("transient test");
            return await Task.FromResult(9);
        }));
        Assert.Equal(2, retryAttempts);
        Assert.Contains("CREATE TABLE", context.Database.GenerateCreateScript());

        Assert.Equal(1, await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"efcore_facade_probe\" (\"Id\", \"Name\") VALUES ({1}, {"one"})"));
        Assert.Equal(1, await context.Database.ExecuteSqlAsync($"INSERT INTO \"efcore_facade_probe\" (\"Id\", \"Name\") VALUES ({2}, {"two"})"));
        Assert.Equal(1, context.Database.ExecuteSqlRaw("UPDATE \"efcore_facade_probe\" SET \"Name\" = {0} WHERE \"Id\" = {1}", "ONE", 1));

        Assert.Equal("ONE", await context.Database.SqlQuery<string>($"SELECT \"Name\" AS \"Value\" FROM \"efcore_facade_probe\" WHERE \"Id\" = {1}").SingleAsync());
        Assert.Equal("two", await context.Database.SqlQueryRaw<string>("SELECT \"Name\" AS \"Value\" FROM \"efcore_facade_probe\" WHERE \"Id\" = {0}", 2).SingleAsync());

        var compiled = EF.CompileAsyncQuery((FacadeDbContext db, int id) => db.Rows.Where(row => row.Id == id).Select(row => row.Name).Single());
        Assert.Equal("ONE", await compiled(context, 1));

        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            await context.Database.ExecuteSqlRawAsync("INSERT INTO \"efcore_facade_probe\" (\"Id\", \"Name\") VALUES (3, 'rollback')");
            await transaction.CreateSavepointAsync("before_four");
            await context.Database.ExecuteSqlRawAsync("INSERT INTO \"efcore_facade_probe\" (\"Id\", \"Name\") VALUES (4, 'savepoint')");
            await transaction.RollbackToSavepointAsync("before_four");
            await transaction.CommitAsync();
        }

        Assert.True(await context.Rows.AnyAsync(row => row.Id == 3));
        Assert.False(await context.Rows.AnyAsync(row => row.Id == 4));

        await using var externalConnection = new KdbndpConnection(connectionString);
        await externalConnection.OpenAsync();
        await using var externalTransaction = await externalConnection.BeginTransactionAsync();
        context.Database.SetDbConnection(externalConnection, contextOwnsConnection: false);
        await context.Database.UseTransactionAsync(externalTransaction);
        await context.Database.ExecuteSqlRawAsync("INSERT INTO \"efcore_facade_probe\" (\"Id\", \"Name\") VALUES (5, 'external')");
        await externalTransaction.RollbackAsync();
        await context.Database.UseTransactionAsync(null);
        Assert.False(await context.Rows.AnyAsync(row => row.Id == 5));

        Assert.True(interceptor.ExecutedCount > 0);
        var lifecycleOptions = new DbContextOptionsBuilder<FacadeDbContext>().UseKdbndp(connectionString).Options;

        using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await using var ambientContext = new FacadeDbContext(lifecycleOptions);
            await ambientContext.Database.ExecuteSqlRawAsync("INSERT INTO \"efcore_facade_probe\" (\"Id\", \"Name\") VALUES (6, 'ambient')");
            scope.Complete();
        }
        await using (var verificationContext = new FacadeDbContext(lifecycleOptions))
        {
            Assert.True(await verificationContext.Rows.AnyAsync(row => row.Id == 6));
        }
        await using var lifecycleContext = new FacadeDbContext(lifecycleOptions);
        await lifecycleContext.Database.OpenConnectionAsync();
        Assert.Equal(ConnectionState.Open, lifecycleContext.Database.GetDbConnection().State);
        await lifecycleContext.Database.CloseConnectionAsync();
        Assert.Equal(ConnectionState.Closed, lifecycleContext.Database.GetDbConnection().State);
    }

    [Fact]
    public async Task Migration_history_upgrade_script_and_rollback_work()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<MigrationDbContext>()
            .UseKdbndp(connectionString, options => options.SetOracleCompatibilityMode())
            .Options;
        await using var context = new MigrationDbContext(options);
        await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS \"efcore_migration_probe\"");
        await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS \"__EFMigrationsHistory\"");

        Assert.Equal(2, (await context.Database.GetPendingMigrationsAsync()).Count());
        await context.Database.MigrateAsync();
        Assert.Equal(2, (await context.Database.GetAppliedMigrationsAsync()).Count());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Equal(7, await context.Rows.Select(row => row.Counter).SingleAsync());

        var migrator = context.GetService<IMigrator>();
        var script = migrator.GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
        Assert.Contains("__EFMigrationsHistory", script);
        Assert.Contains("DO $EF$", script);

        var migrations = context.Database.GetMigrations().ToArray();
        await migrator.MigrateAsync(migrations[0]);
        Assert.Single(await context.Database.GetAppliedMigrationsAsync());
        await migrator.MigrateAsync(Migration.InitialDatabase);
        Assert.Empty(await context.Database.GetAppliedMigrationsAsync());
        Assert.False(await context.Database.SqlQueryRaw<bool>("SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema() AND table_name = 'efcore_migration_probe') AS \"Value\"").SingleAsync());
        await context.Database.ExecuteSqlRawAsync(script);
        await context.Database.ExecuteSqlRawAsync(script);
        Assert.Equal(2, (await context.Database.GetAppliedMigrationsAsync()).Count());
        await migrator.MigrateAsync(Migration.InitialDatabase);
    }

    [Fact]
    public async Task Migration_operation_sql_executes_for_schema_constraints_indexes_sequences_and_data()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        var options = new DbContextOptionsBuilder<FacadeDbContext>().UseKdbndp(connectionString).Options;
        await using var context = new FacadeDbContext(options);
        var generator = context.GetService<IMigrationsSqlGenerator>();
        var operations = new MigrationOperation[]
        {
            new SqlOperation { Sql = "DROP TABLE IF EXISTS \"efcore_operation_child\"; DROP TABLE IF EXISTS \"efcore_operation_parent\"; DROP SEQUENCE IF EXISTS \"efcore_operation_seq\"" },
            new CreateSequenceOperation { Name = "efcore_operation_seq", ClrType = typeof(long), StartValue = 10, IncrementBy = 2 },
            new CreateTableOperation
            {
                Name = "efcore_operation_parent",
                Columns =
                {
                    new AddColumnOperation { Name = "Id", Table = "efcore_operation_parent", ClrType = typeof(int), ColumnType = "integer", IsNullable = false },
                    new AddColumnOperation { Name = "Code", Table = "efcore_operation_parent", ClrType = typeof(string), ColumnType = "text", IsNullable = false }
                },
                PrimaryKey = new AddPrimaryKeyOperation { Name = "PK_operation_parent", Table = "efcore_operation_parent", Columns = ["Id"] },
                UniqueConstraints = { new AddUniqueConstraintOperation { Name = "AK_operation_parent_Code", Table = "efcore_operation_parent", Columns = ["Code"] } },
                CheckConstraints = { new AddCheckConstraintOperation { Name = "CK_operation_parent_Id", Table = "efcore_operation_parent", Sql = "\"Id\" > 0" } }
            },
            new CreateTableOperation
            {
                Name = "efcore_operation_child",
                Columns =
                {
                    new AddColumnOperation { Name = "Id", Table = "efcore_operation_child", ClrType = typeof(int), ColumnType = "integer", IsNullable = false },
                    new AddColumnOperation { Name = "ParentId", Table = "efcore_operation_child", ClrType = typeof(int), ColumnType = "integer", IsNullable = false }
                },
                PrimaryKey = new AddPrimaryKeyOperation { Name = "PK_operation_child", Table = "efcore_operation_child", Columns = ["Id"] },
                ForeignKeys = { new AddForeignKeyOperation { Name = "FK_operation_child_parent", Table = "efcore_operation_child", Columns = ["ParentId"], PrincipalTable = "efcore_operation_parent", PrincipalColumns = ["Id"], OnDelete = ReferentialAction.Cascade } }
            },
            new CreateIndexOperation { Name = "IX_operation_child_ParentId", Table = "efcore_operation_child", Columns = ["ParentId"], IsDescending = [true] },
            new InsertDataOperation { Table = "efcore_operation_parent", Columns = ["Id", "Code"], ColumnTypes = ["integer", "text"], Values = new object[,] { { 1, "A" } } },
            new UpdateDataOperation { Table = "efcore_operation_parent", KeyColumns = ["Id"], KeyColumnTypes = ["integer"], KeyValues = new object[,] { { 1 } }, Columns = ["Code"], ColumnTypes = ["text"], Values = new object[,] { { "B" } } },
            new InsertDataOperation { Table = "efcore_operation_child", Columns = ["Id", "ParentId"], ColumnTypes = ["integer", "integer"], Values = new object[,] { { 1, 1 } } },
            new DeleteDataOperation { Table = "efcore_operation_child", KeyColumns = ["Id"], KeyColumnTypes = ["integer"], KeyValues = new object[,] { { 1 } } }
        };

        foreach (var command in generator.Generate(operations))
        {
            await context.Database.ExecuteSqlRawAsync(command.CommandText);
        }

        Assert.Equal("B", await context.Database.SqlQueryRaw<string>("SELECT \"Code\" AS \"Value\" FROM \"efcore_operation_parent\" WHERE \"Id\" = 1").SingleAsync());

        var constraintChanges = new MigrationOperation[]
        {
            new DropForeignKeyOperation { Table = "efcore_operation_child", Name = "FK_operation_child_parent" },
            new AddForeignKeyOperation { Table = "efcore_operation_child", Name = "FK_operation_child_parent", Columns = ["ParentId"], PrincipalTable = "efcore_operation_parent", PrincipalColumns = ["Id"], OnDelete = ReferentialAction.Cascade },
            new DropPrimaryKeyOperation { Table = "efcore_operation_child", Name = "PK_operation_child" },
            new AddPrimaryKeyOperation { Table = "efcore_operation_child", Name = "PK_operation_child", Columns = ["Id"] },
            new DropUniqueConstraintOperation { Table = "efcore_operation_parent", Name = "AK_operation_parent_Code" },
            new AddUniqueConstraintOperation { Table = "efcore_operation_parent", Name = "AK_operation_parent_Code", Columns = ["Code"] },
            new AlterSequenceOperation { Name = "efcore_operation_seq", IncrementBy = 5, OldSequence = new CreateSequenceOperation { Name = "efcore_operation_seq", ClrType = typeof(long), IncrementBy = 2 } }
        };
        foreach (var command in generator.Generate(constraintChanges)) await context.Database.ExecuteSqlRawAsync(command.CommandText);

        var changes = new MigrationOperation[]
        {
            new AddColumnOperation { Table = "efcore_operation_parent", Name = "Description", ClrType = typeof(string), ColumnType = "text", IsNullable = true },
            new AlterColumnOperation { Table = "efcore_operation_parent", Name = "Description", ClrType = typeof(string), ColumnType = "varchar(50)", IsNullable = false, DefaultValue = "n/a", OldColumn = new AddColumnOperation { Table = "efcore_operation_parent", Name = "Description", ClrType = typeof(string), ColumnType = "text", IsNullable = true } },
            new RenameColumnOperation { Table = "efcore_operation_parent", Name = "Description", NewName = "Details" },
            new AddCheckConstraintOperation { Table = "efcore_operation_parent", Name = "CK_operation_parent_Details", Sql = "length(\"Details\") > 0" },
            new CreateIndexOperation { Table = "efcore_operation_parent", Name = "IX_operation_parent_Details", Columns = ["Details"], Filter = "\"Details\" IS NOT NULL" },
            new RenameIndexOperation { Table = "efcore_operation_parent", Name = "IX_operation_parent_Details", NewName = "IX_operation_parent_Details_Renamed" },
            new AlterTableOperation { Name = "efcore_operation_parent", Comment = "migration table", OldTable = new AlterTableOperation { Name = "efcore_operation_parent" } },
            new RestartSequenceOperation { Name = "efcore_operation_seq", StartValue = 100 },
            new RenameSequenceOperation { Name = "efcore_operation_seq", NewName = "efcore_operation_seq_renamed" }
        };
        foreach (var command in generator.Generate(changes)) await context.Database.ExecuteSqlRawAsync(command.CommandText);
        Assert.Equal("migration table", await context.Database.SqlQueryRaw<string>("SELECT obj_description('efcore_operation_parent'::regclass) AS \"Value\"").SingleAsync());

        var cleanup = new MigrationOperation[]
        {
            new DropIndexOperation { Table = "efcore_operation_parent", Name = "IX_operation_parent_Details_Renamed" },
            new DropCheckConstraintOperation { Table = "efcore_operation_parent", Name = "CK_operation_parent_Details" },
            new DropColumnOperation { Table = "efcore_operation_parent", Name = "Details" },
            new DropSequenceOperation { Name = "efcore_operation_seq_renamed" }
        };
        foreach (var command in generator.Generate(cleanup)) await context.Database.ExecuteSqlRawAsync(command.CommandText);

        var schemaOperations = new MigrationOperation[]
        {
            new SqlOperation { Sql = "DROP SCHEMA IF EXISTS \"efcore_migration_ops_schema\" CASCADE" },
            new EnsureSchemaOperation { Name = "efcore_migration_ops_schema" },
            new CreateTableOperation
            {
                Name = "BeforeRename", Schema = "efcore_migration_ops_schema",
                Columns = { new AddColumnOperation { Name = "Id", Table = "BeforeRename", Schema = "efcore_migration_ops_schema", ClrType = typeof(int), ColumnType = "integer", IsNullable = false } },
                PrimaryKey = new AddPrimaryKeyOperation { Name = "PK_BeforeRename", Table = "BeforeRename", Schema = "efcore_migration_ops_schema", Columns = ["Id"] }
            },
            new RenameTableOperation { Name = "BeforeRename", Schema = "efcore_migration_ops_schema", NewName = "AfterRename" },
            new DropTableOperation { Name = "AfterRename", Schema = "efcore_migration_ops_schema" },
            new DropSchemaOperation { Name = "efcore_migration_ops_schema" }
        };
        foreach (var command in generator.Generate(schemaOperations)) await context.Database.ExecuteSqlRawAsync(command.CommandText);
    }

    private sealed class FacadeDbContext(DbContextOptions<FacadeDbContext> options) : DbContext(options)
    {
        public DbSet<FacadeRow> Rows => Set<FacadeRow>();
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<FacadeRow>(entity => { entity.ToTable("efcore_facade_probe"); entity.HasKey(row => row.Id); });
    }

    private sealed class FacadeRow { public int Id { get; set; } public string Name { get; set; } = ""; }

    private sealed class CountingCommandInterceptor : DbCommandInterceptor
    {
        public int ExecutedCount { get; private set; }
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) { ExecutedCount++; return ValueTask.FromResult(result); }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) { ExecutedCount++; return ValueTask.FromResult(result); }
    }
}

public sealed class MigrationDbContext(DbContextOptions<MigrationDbContext> options) : DbContext(options)
{
    public DbSet<MigrationRow> Rows => Set<MigrationRow>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => ConfigureModel(modelBuilder);
    internal static void ConfigureModel(ModelBuilder modelBuilder) => modelBuilder.Entity<MigrationRow>(entity => { entity.ToTable("efcore_migration_probe"); entity.HasKey(row => row.Id); entity.Property(row => row.Name).HasMaxLength(100); });
}

public sealed class MigrationDbContextFactory : IDesignTimeDbContextFactory<MigrationDbContext>
{
    public MigrationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("KINGBASE_TEST_CONNECTION")
            ?? throw new InvalidOperationException("KINGBASE_TEST_CONNECTION is required for design-time tests.");
        return new MigrationDbContext(new DbContextOptionsBuilder<MigrationDbContext>()
            .UseKdbndp(connectionString, options => options.SetOracleCompatibilityMode())
            .Options);
    }
}

public sealed class MigrationRow { public int Id { get; set; } public string Name { get; set; } = ""; public int Counter { get; set; } }

[DbContext(typeof(MigrationDbContext))]
[Migration("20260804000001_Initial")]
public sealed class InitialMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "efcore_migration_probe",
            columns: table => new { Id = table.Column<int>(type: "integer", nullable: false), Name = table.Column<string>(type: "varchar(100)", nullable: false) },
            constraints: table => table.PrimaryKey("PK_efcore_migration_probe", row => row.Id));
        migrationBuilder.Sql("INSERT INTO \"efcore_migration_probe\" (\"Id\", \"Name\") VALUES (1, 'migration')");
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("efcore_migration_probe");
}

[DbContext(typeof(MigrationDbContext))]
[Migration("20260804000002_AddCounter")]
public sealed class AddCounterMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>("Counter", "efcore_migration_probe", type: "integer", nullable: false, defaultValue: 7);
        migrationBuilder.CreateIndex("IX_efcore_migration_probe_Name", "efcore_migration_probe", "Name");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_efcore_migration_probe_Name", "efcore_migration_probe");
        migrationBuilder.DropColumn("Counter", "efcore_migration_probe");
    }
}

[DbContext(typeof(MigrationDbContext))]
public sealed class MigrationDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) => MigrationDbContext.ConfigureModel(modelBuilder);
}
