using Kdbndp;
using Microsoft.EntityFrameworkCore;

namespace Kingbase.EntityFrameworkCore.Tests.Integration;

[Collection("Kingbase integration")]
public sealed class KingbaseTypeAndSaveIntegrationTests
{
    private const string ConnectionVariable = "KINGBASE_TEST_CONNECTION";

    [Fact]
    public async Task Scalar_clr_types_roundtrip_against_real_kingbase()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using var connection = await OpenConnectionAsync(connectionString);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                DROP TABLE IF EXISTS "efcore_type_probe";
                CREATE TABLE "efcore_type_probe" (
                    "Id" integer NOT NULL PRIMARY KEY,
                    "BoolValue" boolean NOT NULL,
                    "ByteValue" smallint NOT NULL,
                    "SByteValue" smallint NOT NULL,
                    "ShortValue" smallint NOT NULL,
                    "UShortValue" integer NOT NULL,
                    "IntValue" integer NOT NULL,
                    "UIntValue" bigint NOT NULL,
                    "LongValue" bigint NOT NULL,
                    "ULongValue" numeric(20,0) NOT NULL,
                    "FloatValue" real NOT NULL,
                    "DoubleValue" double precision NOT NULL,
                    "DecimalValue" numeric(18,4) NOT NULL,
                    "TextValue" text NOT NULL,
                    "VarCharValue" character varying(32) NOT NULL,
                    "FixedValue" character(4) NOT NULL,
                    "CharValue" character(1) NOT NULL,
                    "GuidValue" uuid NOT NULL,
                    "BytesValue" bytea NOT NULL,
                    "DateOnlyValue" date NOT NULL,
                    "TimeOnlyValue" time without time zone NOT NULL,
                    "TimeSpanValue" interval NOT NULL,
                    "DateTimeValue" timestamp without time zone NOT NULL,
                    "DateTimeOffsetValue" timestamp with time zone NOT NULL,
                    "EnumValue" integer NOT NULL,
                    "NullableIntValue" integer NULL,
                    "NullableDateValue" timestamp without time zone NULL
                );
                """;
            await command.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<TypeDbContext>()
            .UseKdbndp(connection, kingbaseOptionsAction: options => options.SetOracleCompatibilityMode())
            .Options;
        var expectedGuid = Guid.Parse("f50a8191-2f9a-4f9c-a8af-15e6c9818091");
        var expectedDateTime = new DateTime(2026, 8, 4, 12, 34, 56, 789, DateTimeKind.Unspecified);
        var expectedOffset = new DateTimeOffset(2026, 8, 4, 20, 34, 56, TimeSpan.FromHours(8));

        await using (var context = new TypeDbContext(options))
        {
            context.Rows.Add(new TypeRow
            {
                Id = 1,
                BoolValue = true,
                ByteValue = 250,
                SByteValue = -120,
                ShortValue = -32000,
                UShortValue = 65000,
                IntValue = -2_000_000_000,
                UIntValue = 4_000_000_000,
                LongValue = -9_000_000_000_000_000_000,
                ULongValue = 18_000_000_000_000_000_000,
                FloatValue = 123.5f,
                DoubleValue = -98765.125,
                DecimalValue = 1234567890.1234m,
                TextValue = "人大金仓 EF Core 10",
                VarCharValue = "varchar-value",
                FixedValue = "ABCD",
                CharValue = '金',
                GuidValue = expectedGuid,
                BytesValue = [0, 1, 127, 128, 255],
                DateOnlyValue = new DateOnly(2026, 8, 4),
                TimeOnlyValue = new TimeOnly(12, 34, 56, 789),
                TimeSpanValue = TimeSpan.FromDays(2) + TimeSpan.FromMinutes(3),
                DateTimeValue = expectedDateTime,
                DateTimeOffsetValue = expectedOffset,
                EnumValue = ProbeStatus.Active,
                NullableIntValue = null,
                NullableDateValue = null
            });
            Assert.Equal(1, await context.SaveChangesAsync());
        }

        await using (var context = new TypeDbContext(options))
        {
            var row = await context.Rows.AsNoTracking().SingleAsync();
            Assert.True(row.BoolValue);
            Assert.Equal((byte)250, row.ByteValue);
            Assert.Equal((sbyte)-120, row.SByteValue);
            Assert.Equal((short)-32000, row.ShortValue);
            Assert.Equal((ushort)65000, row.UShortValue);
            Assert.Equal(-2_000_000_000, row.IntValue);
            Assert.Equal(4_000_000_000U, row.UIntValue);
            Assert.Equal(-9_000_000_000_000_000_000L, row.LongValue);
            Assert.Equal(18_000_000_000_000_000_000UL, row.ULongValue);
            Assert.Equal(123.5f, row.FloatValue);
            Assert.Equal(-98765.125, row.DoubleValue);
            Assert.Equal(1234567890.1234m, row.DecimalValue);
            Assert.Equal("人大金仓 EF Core 10", row.TextValue);
            Assert.Equal("varchar-value", row.VarCharValue);
            Assert.Equal("ABCD", row.FixedValue);
            Assert.Equal('金', row.CharValue);
            Assert.Equal(expectedGuid, row.GuidValue);
            Assert.Equal([0, 1, 127, 128, 255], row.BytesValue);
            Assert.Equal(new DateOnly(2026, 8, 4), row.DateOnlyValue);
            Assert.Equal(new TimeOnly(12, 34, 56, 789), row.TimeOnlyValue);
            Assert.Equal(TimeSpan.FromDays(2) + TimeSpan.FromMinutes(3), row.TimeSpanValue);
            Assert.Equal(expectedDateTime, row.DateTimeValue);
            Assert.Equal(expectedOffset.UtcDateTime, row.DateTimeOffsetValue.UtcDateTime);
            Assert.Equal(ProbeStatus.Active, row.EnumValue);
            Assert.Null(row.NullableIntValue);
            Assert.Null(row.NullableDateValue);
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "DROP TABLE IF EXISTS \"efcore_type_probe\";";
            await command.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task Save_tracking_batching_concurrency_and_cascade_execute_on_kingbase()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using (var connection = await OpenConnectionAsync(connectionString))
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                DROP TABLE IF EXISTS "efcore_save_child";
                DROP TABLE IF EXISTS "efcore_save_parent";
                CREATE TABLE "efcore_save_parent" (
                    "Id" integer NOT NULL PRIMARY KEY,
                    "Name" text NOT NULL,
                    "Version" integer NOT NULL,
                    "DefaultValue" integer DEFAULT 42 NOT NULL,
                    "ComputedValue" integer GENERATED ALWAYS AS ("Version" * 2) STORED
                );
                CREATE TABLE "efcore_save_child" (
                    "Id" integer NOT NULL PRIMARY KEY,
                    "ParentId" integer NOT NULL,
                    "Name" text NOT NULL,
                    CONSTRAINT "FK_save_child_parent" FOREIGN KEY ("ParentId")
                        REFERENCES "efcore_save_parent" ("Id") ON DELETE CASCADE
                );
                """;
            await command.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<SaveDbContext>()
            .UseKdbndp(connectionString, kingbaseOptionsAction: options => options.SetOracleCompatibilityMode())
            .Options;

        await using (var context = new SaveDbContext(options))
        {
            var parent = new SaveParent
            {
                Id = 1,
                Name = "graph",
                Version = 1,
                Children =
                [
                    new SaveChild { Id = 11, Name = "child-a" },
                    new SaveChild { Id = 12, Name = "child-b" }
                ]
            };
            await context.AddAsync(parent);
            context.AddRange(
                new SaveParent { Id = 2, Name = "range-a", Version = 1 },
                new SaveParent { Id = 3, Name = "range-b", Version = 1 });

            Assert.All(parent.Children, child => Assert.Same(parent, child.Parent));
            Assert.Equal(5, await context.SaveChangesAsync());
            Assert.All(context.ChangeTracker.Entries(), entry => Assert.Equal(EntityState.Unchanged, entry.State));
            Assert.Equal(42, parent.DefaultValue);
            Assert.Equal(2, parent.ComputedValue);
        }

        await using (var context = new SaveDbContext(options))
        {
            var parent = await context.Parents.Include(row => row.Children).SingleAsync(row => row.Id == 1);
            Assert.Equal(2, parent.Children.Count);
            Assert.All(parent.Children, child => Assert.Same(parent, child.Parent));

            context.ChangeTracker.AutoDetectChangesEnabled = false;
            parent.Name = "detected";
            Assert.Equal(EntityState.Unchanged, context.Entry(parent).State);
            context.ChangeTracker.DetectChanges();
            Assert.Equal(EntityState.Modified, context.Entry(parent).State);
            Assert.Equal(1, await context.SaveChangesAsync());
        }

        await using (var context = new SaveDbContext(options))
        {
            var first = new SaveParent { Id = 2, Name = "attached-a", Version = 1 };
            var second = new SaveParent { Id = 3, Name = "attached-b", Version = 1 };
            context.AttachRange(first, second);
            Assert.Equal(EntityState.Unchanged, context.Entry(first).State);
            first.Name = "updated-a";
            second.Name = "updated-b";
            context.UpdateRange(first, second);
            Assert.Equal(2, await context.SaveChangesAsync());
        }

        await using (var firstContext = new SaveDbContext(options))
        await using (var secondContext = new SaveDbContext(options))
        {
            var first = await firstContext.Parents.SingleAsync(row => row.Id == 1);
            var second = await secondContext.Parents.SingleAsync(row => row.Id == 1);
            first.Name = "first-writer";
            first.Version++;
            await firstContext.SaveChangesAsync();
            second.Name = "stale-writer";
            second.Version++;
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());
        }

        await using (var context = new SaveDbContext(options))
        {
            var parents = await context.Parents.OrderBy(row => row.Id).ToListAsync();
            context.RemoveRange(parents);
            Assert.Equal(3, await context.SaveChangesAsync());
            Assert.False(await context.Parents.AnyAsync());
            Assert.False(await context.Children.AnyAsync());
        }

        await using (var connection = await OpenConnectionAsync(connectionString))
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                DROP TABLE IF EXISTS "efcore_save_child";
                DROP TABLE IF EXISTS "efcore_save_parent";
                """;
            await command.ExecuteNonQueryAsync();
        }
    }

    private static async Task<KdbndpConnection> OpenConnectionAsync(string connectionString)
    {
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

        throw new InvalidOperationException("Unable to connect to the KingbaseES integration database after three attempts.", lastException);
    }

    private sealed class TypeDbContext(DbContextOptions<TypeDbContext> options) : DbContext(options)
    {
        public DbSet<TypeRow> Rows => Set<TypeRow>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TypeRow>(entity =>
            {
                entity.ToTable("efcore_type_probe");
                entity.HasKey(row => row.Id);
                entity.Property(row => row.Id).ValueGeneratedNever();
                entity.Property(row => row.DecimalValue).HasPrecision(18, 4);
                entity.Property(row => row.VarCharValue).HasMaxLength(32);
                entity.Property(row => row.FixedValue).HasMaxLength(4).IsFixedLength();
            });
        }
    }

    private sealed class TypeRow
    {
        public int Id { get; set; }
        public bool BoolValue { get; set; }
        public byte ByteValue { get; set; }
        public sbyte SByteValue { get; set; }
        public short ShortValue { get; set; }
        public ushort UShortValue { get; set; }
        public int IntValue { get; set; }
        public uint UIntValue { get; set; }
        public long LongValue { get; set; }
        public ulong ULongValue { get; set; }
        public float FloatValue { get; set; }
        public double DoubleValue { get; set; }
        public decimal DecimalValue { get; set; }
        public string TextValue { get; set; } = string.Empty;
        public string VarCharValue { get; set; } = string.Empty;
        public string FixedValue { get; set; } = string.Empty;
        public char CharValue { get; set; }
        public Guid GuidValue { get; set; }
        public byte[] BytesValue { get; set; } = [];
        public DateOnly DateOnlyValue { get; set; }
        public TimeOnly TimeOnlyValue { get; set; }
        public TimeSpan TimeSpanValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public DateTimeOffset DateTimeOffsetValue { get; set; }
        public ProbeStatus EnumValue { get; set; }
        public int? NullableIntValue { get; set; }
        public DateTime? NullableDateValue { get; set; }
    }

    private enum ProbeStatus
    {
        Inactive,
        Active
    }

    private sealed class SaveDbContext(DbContextOptions<SaveDbContext> options) : DbContext(options)
    {
        public DbSet<SaveParent> Parents => Set<SaveParent>();
        public DbSet<SaveChild> Children => Set<SaveChild>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SaveParent>(entity =>
            {
                entity.ToTable("efcore_save_parent");
                entity.HasKey(row => row.Id);
                entity.Property(row => row.Id).ValueGeneratedNever();
                entity.Property(row => row.Version).IsConcurrencyToken();
                entity.Property(row => row.DefaultValue).HasDefaultValue(42).ValueGeneratedOnAdd();
                entity.Property(row => row.ComputedValue).HasComputedColumnSql("\"Version\" * 2", stored: true);
                entity.HasMany(row => row.Children)
                    .WithOne(row => row.Parent)
                    .HasForeignKey(row => row.ParentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SaveChild>(entity =>
            {
                entity.ToTable("efcore_save_child");
                entity.HasKey(row => row.Id);
                entity.Property(row => row.Id).ValueGeneratedNever();
            });
        }
    }

    private sealed class SaveParent
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Version { get; set; }
        public int DefaultValue { get; set; }
        public int ComputedValue { get; set; }
        public List<SaveChild> Children { get; set; } = [];
    }

    private sealed class SaveChild
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public SaveParent Parent { get; set; } = null!;
    }
}
