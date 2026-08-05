using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using Kdbndp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Kingbase.EntityFrameworkCore.Metadata.Internal;
using System.Text.Json;
using KdbndpTypes;

namespace Kingbase.EntityFrameworkCore.Tests.Integration;

public sealed class KingbaseRemainingCoverageIntegrationTests
{
    private const string ConnectionVariable = "KINGBASE_TEST_CONNECTION";

    [Fact]
    public async Task Optional_struct_complex_equality_and_bulk_assignment_work()
    {
        var fixture = await OpenAsync();
        if (fixture is null) return;
        await using var connection = fixture;
        await ExecuteAsync(connection,
            """
            DROP TABLE IF EXISTS "efcore_complex_coverage";
            CREATE TABLE "efcore_complex_coverage" (
                "Id" integer PRIMARY KEY,
                "OptionalCity" text NULL,
                "OptionalStreet" text NULL,
                "OptionalAddress_OptionalExists" boolean NULL,
                "ContactEmail" text NOT NULL,
                "ContactPhone" text NOT NULL
            );
            """);

        var options = new DbContextOptionsBuilder<ComplexCoverageContext>().UseKdbndp(connection).Options;
        await using var context = new ComplexCoverageContext(options);
        context.Entities.AddRange(
            new ComplexCoverageEntity { Id = 1, OptionalAddress = null, Contact = new ContactValue("a@example.com", "100") },
            new ComplexCoverageEntity { Id = 2, OptionalAddress = new OptionalAddress { City = "Beijing", Street = "Road" }, Contact = new ContactValue("b@example.com", "200") });
        Assert.Equal(2, await context.SaveChangesAsync());
        context.ChangeTracker.Clear();

        Assert.Null((await context.Entities.SingleAsync(entity => entity.Id == 1)).OptionalAddress);
        Assert.Equal("Beijing", (await context.Entities.SingleAsync(entity => entity.Id == 2)).OptionalAddress?.City);
        var expected = new ContactValue("b@example.com", "200");
        Assert.Equal(2, await context.Entities.Where(entity => entity.Contact == expected).Select(entity => entity.Id).SingleAsync());

        Assert.Equal(1, await context.Entities.Where(entity => entity.Id == 2)
            .ExecuteUpdateAsync(setters => setters.SetProperty(entity => entity.Contact, new ContactValue("updated@example.com", "300"))));
        context.ChangeTracker.Clear();
        Assert.Equal("updated@example.com", (await context.Entities.SingleAsync(entity => entity.Id == 2)).Contact.Email);
    }

    [Fact]
    public async Task Seeding_notification_tracking_and_connection_replacement_work()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        builder["Database"] = "efcore_remaining_seed_probe";
        var syncSeeds = 0;
        var asyncSeeds = 0;
        var options = new DbContextOptionsBuilder<SeedContext>()
            .UseKdbndp(builder.ConnectionString, options => options.UseAdminDatabase("template1"))
            .UseSeeding((context, _) =>
            {
                syncSeeds++;
                if (!context.Set<SeedEntity>().Any())
                {
                    context.Add(new SeedEntity { Id = 1, Name = "sync" });
                    context.SaveChanges();
                }
            })
            .UseAsyncSeeding(async (context, _, cancellationToken) =>
            {
                asyncSeeds++;
                if (!await context.Set<SeedEntity>().AnyAsync(cancellationToken))
                {
                    context.Add(new SeedEntity { Id = 1, Name = "async" });
                    await context.SaveChangesAsync(cancellationToken);
                }
            })
            .Options;

        await using (var cleanup = new SeedContext(options)) await cleanup.Database.EnsureDeletedAsync();
        await using (var context = new SeedContext(options))
        {
            Assert.True(await context.Database.EnsureCreatedAsync());
            Assert.Equal("async", await context.Entities.Select(entity => entity.Name).SingleAsync());
        }
        using (var context = new SeedContext(options))
        {
            Assert.False(context.Database.EnsureCreated());
            Assert.Single(context.Entities);
        }
        Assert.True(syncSeeds > 0);
        Assert.True(asyncSeeds > 0);
        await using (var cleanup = new SeedContext(options)) await cleanup.Database.EnsureDeletedAsync();

        await using var connection = await OpenAsync();
        Assert.NotNull(connection);
        await ExecuteAsync(connection!, "DROP TABLE IF EXISTS \"efcore_notification_probe\"; CREATE TABLE \"efcore_notification_probe\" (\"Id\" integer PRIMARY KEY, \"Name\" text NOT NULL);");
        var notificationOptions = new DbContextOptionsBuilder<NotificationContext>().UseKdbndp(connection!).Options;
        await using (var context = new NotificationContext(notificationOptions))
        {
            var entity = new NotificationEntity { Id = 1, Name = "before" };
            context.Add(entity);
            await context.SaveChangesAsync();
            entity.Name = "after";
            Assert.Equal(EntityState.Modified, context.Entry(entity).State);
            await context.SaveChangesAsync();
        }
        await using (var context = new NotificationContext(notificationOptions))
        {
            Assert.Equal("after", await context.Entities.Select(entity => entity.Name).SingleAsync());
        }

        var replacementOptions = new DbContextOptionsBuilder<NotificationContext>().UseKdbndp((string?)null).Options;
        await using var replacementContext = new NotificationContext(replacementOptions);
        replacementContext.Database.SetConnectionString(connectionString);
        Assert.True(await replacementContext.Database.CanConnectAsync());
        await replacementContext.Database.OpenConnectionAsync();
        Assert.Throws<InvalidOperationException>(() => replacementContext.Database.SetConnectionString(connectionString));
        await replacementContext.Database.CloseConnectionAsync();
        replacementContext.Database.SetConnectionString(connectionString);
    }

    [Fact]
    public async Task Query_transaction_interceptors_and_connection_pool_concurrency_work()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        await using var setup = await OpenAsync();
        Assert.NotNull(setup);
        await ExecuteAsync(setup!, "DROP TABLE IF EXISTS \"efcore_interceptor_probe\"; CREATE TABLE \"efcore_interceptor_probe\" (\"Id\" integer PRIMARY KEY, \"Name\" text NOT NULL); INSERT INTO \"efcore_interceptor_probe\" VALUES (1, 'one');");

        var queryInterceptor = new CountingQueryInterceptor();
        var transactionInterceptor = new CountingTransactionInterceptor();
        var options = new DbContextOptionsBuilder<InterceptorContext>()
            .UseKdbndp(connectionString)
            .AddInterceptors(queryInterceptor, transactionInterceptor)
            .Options;
        await using (var context = new InterceptorContext(options))
        {
            Assert.Equal("one", await context.Entities.Select(entity => entity.Name).SingleAsync());
            await using var transaction = await context.Database.BeginTransactionAsync();
            await transaction.CreateSavepointAsync("remaining_coverage");
            await transaction.CommitAsync();
        }
        Assert.True(queryInterceptor.CompilationCount > 0);
        Assert.True(transactionInterceptor.StartedCount > 0);
        Assert.True(transactionInterceptor.CommittedCount > 0);
        Assert.True(transactionInterceptor.SavepointCount > 0);

        var pooledBuilder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        pooledBuilder["Maximum Pool Size"] = 4;
        pooledBuilder["Timeout"] = 20;
        var tasks = Enumerable.Range(0, 40).Select(async _ =>
        {
            await using var connection = new KdbndpConnection(pooledBuilder.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        });
        Assert.All(await Task.WhenAll(tasks), value => Assert.Equal(1, value));
    }

    [Fact]
    public async Task Index_include_fluent_api_and_migration_sql_work()
    {
        await using var connection = await OpenAsync();
        if (connection is null) return;
        await ExecuteAsync(connection, "DROP TABLE IF EXISTS \"efcore_include_coverage\"; CREATE TABLE \"efcore_include_coverage\" (\"Id\" integer PRIMARY KEY, \"Code\" text NOT NULL, \"Payload\" text NULL);");
        var options = new DbContextOptionsBuilder<IncludeContext>().UseKdbndp(connection).Options;
        await using var context = new IncludeContext(options);
        var index = context.Model.FindEntityType(typeof(IncludeEntity))!.GetIndexes().Single();
        Assert.Equal(["Payload"], (string[])index[KingbaseAnnotationNames.IndexInclude]!);

        var operation = new CreateIndexOperation { Name = "IX_include_coverage_Code", Table = "efcore_include_coverage", Columns = ["Code"], Filter = "\"Code\" IS NOT NULL" };
        operation[KingbaseAnnotationNames.IndexInclude] = new[] { "Payload" };
        var generator = context.GetService<IMigrationsSqlGenerator>();
        var sql = Assert.Single(generator.Generate([operation])).CommandText;
        Assert.Contains("INCLUDE (\"Payload\")", sql);
        await context.Database.ExecuteSqlRawAsync(sql);
        Assert.True(await context.Database.SqlQueryRaw<bool>("SELECT EXISTS (SELECT 1 FROM sys_indexes WHERE indexname = 'IX_include_coverage_Code' AND indexdef LIKE '%INCLUDE%') AS \"Value\"").SingleAsync());
    }

    [Fact]
    public async Task Json_array_list_range_and_fulltext_types_roundtrip()
    {
        await using var connection = await OpenAsync();
        if (connection is null) return;
        await ExecuteAsync(connection,
            """
            DROP TABLE IF EXISTS "efcore_advanced_mapping_coverage";
            CREATE TABLE "efcore_advanced_mapping_coverage" (
                "Id" integer PRIMARY KEY,
                "Document" jsonb NOT NULL,
                "Element" jsonb NOT NULL,
                "Numbers" integer[] NOT NULL,
                "NumberList" integer[] NOT NULL,
                "Period" int4range NOT NULL,
                "Search" tsvector NOT NULL
            );
            """);
        var options = new DbContextOptionsBuilder<AdvancedTypeContext>().UseKdbndp(connection).Options;
        await using var context = new AdvancedTypeContext(options);
        using var document = JsonDocument.Parse("{\"name\":\"kingbase\",\"version\":9}");
        var element = JsonDocument.Parse("{\"enabled\":true}").RootElement.Clone();
        await using var vectorCommand = connection.CreateCommand();
        vectorCommand.CommandText = "SELECT to_tsvector('simple', 'hello kingbase')";
        await using var vectorReader = await vectorCommand.ExecuteReaderAsync();
        Assert.True(await vectorReader.ReadAsync());
        var searchVector = vectorReader.GetFieldValue<KdbndpTsVector>(0);
        await vectorReader.DisposeAsync();
        context.Entities.Add(new AdvancedTypeEntity
        {
            Id = 1,
            Document = document,
            Element = element,
            Numbers = [1, 2, 3],
            NumberList = [4, 5, 6],
            Period = new KdbndpRange<int>(1, true, 10, false),
            Search = searchVector
        });
        Assert.Equal(1, await context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        var value = await context.Entities.SingleAsync();
        Assert.Equal("kingbase", value.Document.RootElement.GetProperty("name").GetString());
        Assert.True(value.Element.GetProperty("enabled").GetBoolean());
        Assert.Equal([1, 2, 3], value.Numbers);
        Assert.Equal([4, 5, 6], value.NumberList);
        Assert.Equal(1, value.Period.LowerBound);
        Assert.Equal(10, value.Period.UpperBound);
        Assert.Contains("kingbase", value.Search.ToString());

        var matched = await context.Entities
            .Where(row =>
                EF.Functions.JsonExtractPathText(row.Document, "name") == "kingbase"
                && EF.Functions.JsonExtractPathText(row.Element, "enabled") == "true"
                && EF.Functions.ArrayContains(row.Numbers, 2)
                && EF.Functions.ArrayContains(row.NumberList, 5)
                && EF.Functions.ArrayContains(row.Numbers, new[] { 2, 3 })
                && EF.Functions.ArrayOverlaps(row.Numbers, new[] { 3, 9 })
                && EF.Functions.ArrayLength(row.Numbers) == 3
                && EF.Functions.ArrayLength(row.NumberList) == 3
                && EF.Functions.RangeContains(row.Period, 5)
                && EF.Functions.RangeContains(row.Period, new KdbndpRange<int>(2, true, 4, false))
                && EF.Functions.RangeOverlaps(row.Period, new KdbndpRange<int>(9, true, 12, false))
                && EF.Functions.FullTextMatches(row.Search, "kingbase")
                && EF.Functions.FullTextMatches(row.Search, "simple", "hello"))
            .SingleAsync();
        Assert.Equal(1, matched.Id);
    }

    [Fact]
    public async Task Data_source_and_single_host_multi_host_source_work()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        await using var dataSource = new KdbndpDataSourceBuilder(connectionString).Build();
        var options = new DbContextOptionsBuilder<DataSourceContext>().UseKdbndp(dataSource).Options;
        await using (var first = new DataSourceContext(options))
        await using (var second = new DataSourceContext(options))
        {
            Assert.Equal(1, await first.Database.SqlQueryRaw<int>("SELECT 1 AS \"Value\"").SingleAsync());
            Assert.Equal(2, await second.Database.SqlQueryRaw<int>("SELECT 2 AS \"Value\"").SingleAsync());
            Assert.NotSame(first.Database.GetDbConnection(), second.Database.GetDbConnection());
        }

        await using var multiHostDataSource = new KdbndpDataSourceBuilder(connectionString).BuildMultiHost();
        var multiHostOptions = new DbContextOptionsBuilder<DataSourceContext>().UseKdbndp(multiHostDataSource).Options;
        await using var multiHostContext = new DataSourceContext(multiHostOptions);
        Assert.Equal(3, await multiHostContext.Database.SqlQueryRaw<int>("SELECT 3 AS \"Value\"").SingleAsync());
    }

    private static async Task<KdbndpConnection?> OpenAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString)) return null;
        Exception? lastException = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var connection = new KdbndpConnection(connectionString);
            try
            {
                await connection.OpenAsync();
                return connection;
            }
            catch (Exception exception) when (attempt < 3)
            {
                lastException = exception;
                await connection.DisposeAsync();
                await Task.Delay(TimeSpan.FromSeconds(attempt));
            }
        }
        throw new InvalidOperationException("Unable to connect to KingbaseES after three attempts.", lastException);
    }

    private static async Task ExecuteAsync(KdbndpConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private sealed class ComplexCoverageContext(DbContextOptions<ComplexCoverageContext> options) : DbContext(options)
    {
        public DbSet<ComplexCoverageEntity> Entities => Set<ComplexCoverageEntity>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ComplexCoverageEntity>(entity =>
            {
                entity.ToTable("efcore_complex_coverage");
                entity.HasKey(value => value.Id);
                entity.Property(value => value.Id).ValueGeneratedNever();
                entity.ComplexProperty(value => value.OptionalAddress, complex =>
                {
                    complex.IsRequired(false);
                    complex.HasDiscriminator<bool>("OptionalExists").HasValue(true);
                    complex.Property(value => value.City).HasColumnName("OptionalCity");
                    complex.Property(value => value.Street).HasColumnName("OptionalStreet");
                });
                entity.ComplexProperty(value => value.Contact, complex =>
                {
                    complex.Property(value => value.Email).HasColumnName("ContactEmail");
                    complex.Property(value => value.Phone).HasColumnName("ContactPhone");
                });
            });
        }
    }

    private sealed class ComplexCoverageEntity
    {
        public int Id { get; set; }
        public OptionalAddress? OptionalAddress { get; set; }
        public ContactValue Contact { get; set; }
    }

    private sealed class OptionalAddress
    {
        public string? City { get; set; }
        public string? Street { get; set; }
    }

    private readonly record struct ContactValue(string Email, string Phone);

    private sealed class SeedContext(DbContextOptions<SeedContext> options) : DbContext(options)
    {
        public DbSet<SeedEntity> Entities => Set<SeedEntity>();
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<SeedEntity>(entity => { entity.ToTable("efcore_seed_probe"); entity.HasKey(value => value.Id); entity.Property(value => value.Id).ValueGeneratedNever(); });
    }

    private sealed class SeedEntity { public int Id { get; set; } public string Name { get; set; } = ""; }

    private sealed class NotificationContext(DbContextOptions<NotificationContext> options) : DbContext(options)
    {
        public DbSet<NotificationEntity> Entities => Set<NotificationEntity>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasChangeTrackingStrategy(ChangeTrackingStrategy.ChangingAndChangedNotifications);
            modelBuilder.Entity<NotificationEntity>(entity => { entity.ToTable("efcore_notification_probe"); entity.HasKey(value => value.Id); entity.Property(value => value.Id).ValueGeneratedNever(); });
        }
    }

    private sealed class NotificationEntity : INotifyPropertyChanging, INotifyPropertyChanged
    {
        private int _id;
        private string _name = "";
        public int Id { get => _id; set => Set(ref _id, value, nameof(Id)); }
        public string Name { get => _name; set => Set(ref _name, value, nameof(Name)); }
        public event PropertyChangingEventHandler? PropertyChanging;
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Set<T>(ref T field, T value, string name)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(name));
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    private sealed class InterceptorContext(DbContextOptions<InterceptorContext> options) : DbContext(options)
    {
        public DbSet<InterceptorEntity> Entities => Set<InterceptorEntity>();
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<InterceptorEntity>(entity => { entity.ToTable("efcore_interceptor_probe"); entity.HasKey(value => value.Id); });
    }
    private sealed class InterceptorEntity { public int Id { get; set; } public string Name { get; set; } = ""; }

    private sealed class CountingQueryInterceptor : IQueryExpressionInterceptor
    {
        public int CompilationCount { get; private set; }
        public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData) { CompilationCount++; return queryExpression; }
    }

    private sealed class CountingTransactionInterceptor : DbTransactionInterceptor
    {
        public int StartedCount { get; private set; }
        public int CommittedCount { get; private set; }
        public int SavepointCount { get; private set; }
        public override ValueTask<DbTransaction> TransactionStartedAsync(DbConnection connection, TransactionEndEventData eventData, DbTransaction result, CancellationToken cancellationToken = default) { StartedCount++; return ValueTask.FromResult(result); }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default) { CommittedCount++; return Task.CompletedTask; }
        public override Task CreatedSavepointAsync(DbTransaction transaction, TransactionEventData eventData, CancellationToken cancellationToken = default) { SavepointCount++; return Task.CompletedTask; }
    }

    private sealed class IncludeContext(DbContextOptions<IncludeContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<IncludeEntity>(entity =>
            {
                entity.ToTable("efcore_include_coverage");
                entity.HasKey(value => value.Id);
                entity.HasIndex(value => value.Code).IncludeProperties(value => value.Payload);
            });
    }
    private sealed class IncludeEntity { public int Id { get; set; } public string Code { get; set; } = ""; public string? Payload { get; set; } }

    private sealed class AdvancedTypeContext(DbContextOptions<AdvancedTypeContext> options) : DbContext(options)
    {
        public DbSet<AdvancedTypeEntity> Entities => Set<AdvancedTypeEntity>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<AdvancedTypeEntity>(entity =>
            {
                entity.ToTable("efcore_advanced_mapping_coverage");
                entity.HasKey(value => value.Id);
                entity.Property(value => value.Id).ValueGeneratedNever();
                entity.Property(value => value.Document).HasColumnType("jsonb");
                entity.Property(value => value.Element).HasColumnType("jsonb");
                entity.Property(value => value.Numbers).HasColumnType("integer[]");
                entity.Property(value => value.NumberList).HasColumnType("integer[]");
                entity.Property(value => value.Period).HasColumnType("int4range");
                entity.Property(value => value.Search).HasColumnType("tsvector");
            });
    }
    private sealed class DataSourceContext(DbContextOptions<DataSourceContext> options) : DbContext(options);
    private sealed class AdvancedTypeEntity
    {
        public int Id { get; set; }
        public JsonDocument Document { get; set; } = null!;
        public JsonElement Element { get; set; }
        public int[] Numbers { get; set; } = [];
        public List<int> NumberList { get; set; } = [];
        public KdbndpRange<int> Period { get; set; }
        public KdbndpTsVector Search { get; set; } = null!;
    }
}
