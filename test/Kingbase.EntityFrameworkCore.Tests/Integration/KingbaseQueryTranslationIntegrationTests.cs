using Kdbndp;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace Kingbase.EntityFrameworkCore.Tests.Integration;

[Collection("Kingbase integration")]
public sealed class KingbaseQueryTranslationIntegrationTests
{
    private const string ConnectionVariable = "KINGBASE_TEST_CONNECTION";

    [Fact]
    public async Task String_translations_execute_with_dotnet_semantics()
    {
        await using var fixture = await QueryFixture.CreateAsync();
        if (fixture is null)
        {
            return;
        }

        await using var context = fixture.CreateContext();

        var result = await context.Rows
            .Where(row => row.Text.Contains('p') && row.Text.StartsWith(" A") && row.Text.EndsWith("a "))
            .Select(row => new
            {
                Length = row.Text.Length,
                Lower = row.Text.ToLower(),
                Upper = row.Text.ToUpper(),
                Substring = row.Text.Substring(1, 5),
                Index = row.Text.IndexOf("pha"),
                Replaced = row.Text.Replace("Alpha", "Beta"),
                Trimmed = row.Text.Trim(),
                Concatenated = string.Concat(row.Text.Trim(), "!")
            })
            .SingleAsync();

        Assert.Equal(7, result.Length);
        Assert.Equal(" alpha ", result.Lower);
        Assert.Equal(" ALPHA ", result.Upper);
        Assert.Equal("Alpha", result.Substring);
        Assert.Equal(3, result.Index);
        Assert.Equal(" Beta ", result.Replaced);
        Assert.Equal("Alpha", result.Trimmed);
        Assert.Equal("Alpha!", result.Concatenated);
    }

    [Fact]
    public async Task Date_part_translations_execute_on_kingbase()
    {
        await using var fixture = await QueryFixture.CreateAsync();
        if (fixture is null)
        {
            return;
        }

        await using var context = fixture.CreateContext();
        var result = await context.Rows
            .Where(row => row.Id == 1)
            .Select(row => new
            {
                row.OccurredAt.Year,
                row.OccurredAt.Month,
                row.OccurredAt.Day,
                row.OccurredAt.Hour,
                row.OccurredAt.Minute,
                row.OccurredAt.Second
            })
            .SingleAsync();

        Assert.Equal(2026, result.Year);
        Assert.Equal(8, result.Month);
        Assert.Equal(4, result.Day);
        Assert.Equal(13, result.Hour);
        Assert.Equal(14, result.Minute);
        Assert.Equal(15, result.Second);
    }

    [Fact]
    public async Task Math_translations_execute_on_kingbase()
    {
        await using var fixture = await QueryFixture.CreateAsync();
        if (fixture is null)
        {
            return;
        }

        await using var context = fixture.CreateContext();
        var result = await context.Rows
            .Where(row => row.Id == 1)
            .Select(row => new
            {
                Absolute = Math.Abs(row.Number),
                Ceiling = Math.Ceiling(row.Number),
                Floor = Math.Floor(row.Number),
                Rounded = Math.Round(row.Number),
                SquareRoot = Math.Sqrt(Math.Abs(row.Number)),
                CubeRoot = Math.Cbrt(Math.Abs(row.Number)),
                Log2 = Math.Log2(Math.Abs(row.Number)),
                Maximum = Math.Max(row.Number, 2.0),
                Minimum = Math.Min(row.Number, 2.0),
                Clamped = Math.Clamp(row.Number, -10.0, 10.0),
                Hyperbolic = Math.Tanh(row.Number)
            })
            .SingleAsync();

        Assert.Equal(12.75, result.Absolute, 8);
        Assert.Equal(-12, result.Ceiling);
        Assert.Equal(-13, result.Floor);
        Assert.Equal(-13, result.Rounded);
        Assert.Equal(Math.Sqrt(12.75), result.SquareRoot, 8);
        Assert.Equal(Math.Cbrt(12.75), result.CubeRoot, 8);
        Assert.Equal(Math.Log2(12.75), result.Log2, 8);
        Assert.Equal(2.0, result.Maximum, 8);
        Assert.Equal(-12.75, result.Minimum, 8);
        Assert.Equal(-10.0, result.Clamped, 8);
        Assert.Equal(Math.Tanh(-12.75), result.Hyperbolic, 8);
    }

    [Fact]
    public async Task Core_expression_operators_execute_with_dotnet_semantics()
    {
        await using var fixture = await QueryFixture.CreateAsync();
        if (fixture is null)
        {
            return;
        }

        await using var context = fixture.CreateContext();
        var result = await context.Rows
            .Where(row => row.Id == 1 && row.Number < 0 && !(row.Text == "missing"))
            .Select(row => new
            {
                Comparison = row.Number <= -12.75 && row.Id != 2,
                Arithmetic = ((row.Amount + 2m) * 3m - 1m) / 2m,
                Modulo = row.Id % 2,
                Conditional = row.Number < 0 ? "negative" : "positive",
                Coalesced = row.NullableNumber ?? 42,
                Concatenated = row.Text.Trim() + "!",
                Equal = row.Text.Trim().Equals("Alpha")
            })
            .SingleAsync();

        Assert.True(result.Comparison);
        Assert.Equal(18.625m, result.Arithmetic);
        Assert.Equal(1, result.Modulo);
        Assert.Equal("negative", result.Conditional);
        Assert.Equal(42, result.Coalesced);
        Assert.Equal("Alpha!", result.Concatenated);
        Assert.True(result.Equal);
    }

    [Fact]
    public async Task Relational_functions_guid_regex_enum_and_binary_execute_on_kingbase()
    {
        await using var fixture = await QueryFixture.CreateAsync();
        if (fixture is null)
        {
            return;
        }

        await using var context = fixture.CreateContext();
        Assert.True(await context.Rows.AnyAsync(row => EF.Functions.Like(row.Text, "%Alpha%")));
        Assert.True(await context.Rows.AnyAsync(row => EF.Functions.Collate(row.Text.Trim(), "c") == "Alpha"));
        Assert.True(await context.Rows.AnyAsync(row => Regex.IsMatch(row.Text.Trim(), "^alpha$", RegexOptions.IgnoreCase)));
        Assert.True(await context.Rows.AnyAsync(row => row.Flags.HasFlag(QueryFlags.Read)));
        Assert.True(await context.Rows.AnyAsync(row => row.Bytes.SequenceEqual(row.BytesCopy)));
        var expectedBytes = new byte[] { 1, 2, 3 };
        Assert.True(await context.Rows.AnyAsync(row => row.Bytes.SequenceEqual(expectedBytes)));
        Assert.True(await context.Rows.AnyAsync(row => row.Flags.ToString() == "Read"));

        var generated = await context.Rows.Where(row => row.Id == 1).Select(_ => Guid.NewGuid()).SingleAsync();
        Assert.NotEqual(Guid.Empty, generated);

        var random = await context.Rows.Where(row => row.Id == 1).Select(_ => EF.Functions.Random()).SingleAsync();
        Assert.InRange(random, 0.0, 1.0);
    }

    [Fact]
    public async Task Microsecond_nanosecond_null_compensation_and_parameter_collections_execute_on_kingbase()
    {
        await using var fixture = await QueryFixture.CreateAsync();
        if (fixture is null) return;
        await using var context = fixture.CreateContext();
        await context.Database.ExecuteSqlRawAsync("UPDATE \"efcore_query_translation_probe\" SET \"OccurredAt\" = TIMESTAMP '2026-08-04 13:14:15.123456', \"EventTime\" = TIME '13:14:15.123456' WHERE \"Id\" = 1");

        var parts = await context.Rows.Where(row => row.Id == 1).Select(row => new
        {
            DateMicrosecond = row.OccurredAt.Microsecond,
            DateNanosecond = row.OccurredAt.Nanosecond,
            TimeMicrosecond = row.EventTime.Microsecond,
            TimeNanosecond = row.EventTime.Nanosecond
        }).SingleAsync();
        Assert.Equal(456, parts.DateMicrosecond);
        Assert.Equal(0, parts.DateNanosecond);
        Assert.Equal(456, parts.TimeMicrosecond);
        Assert.Equal(0, parts.TimeNanosecond);
        Assert.Equal(new DateOnly(2026, 8, 4).DayNumber, await context.Rows.Where(row => row.Id == 1).Select(row => row.EventDate.DayNumber).SingleAsync());

        Assert.Equal(2, await context.Rows.CountAsync(row => row.NullableNumber != 7));
        Assert.Empty(await context.Rows.Where(row => Array.Empty<int>().Contains(row.Id)).ToListAsync());
        var ids = Enumerable.Range(1, 128).ToArray();
        var largeCollectionQuery = context.Rows.Where(row => ids.Contains(row.Id));
        Assert.Equal(3, await largeCollectionQuery.CountAsync());
    }

    [Fact]
    public async Task Date_time_methods_and_current_time_execute_on_kingbase()
    {
        await using var fixture = await QueryFixture.CreateAsync();
        if (fixture is null)
        {
            return;
        }

        await using var context = fixture.CreateContext();
        var result = await context.Rows.Where(row => row.Id == 1).Select(row => new
        {
            PlusYear = row.OccurredAt.AddYears(1),
            PlusMonth = row.OccurredAt.AddMonths(1),
            PlusDay = row.OccurredAt.AddDays(1),
            PlusHour = row.OccurredAt.AddHours(2),
            DateOnlyPlus = row.EventDate.AddMonths(1).AddDays(2),
            TimeOnlyPlus = row.EventTime.AddHours(2).AddMinutes(15),
            Combined = row.EventDate.ToDateTime(row.EventTime),
            DateFromTimestamp = DateOnly.FromDateTime(row.OccurredAt),
            TimeFromTimestamp = TimeOnly.FromDateTime(row.OccurredAt),
            LocalNow = DateTime.Now,
            UtcNow = DateTime.UtcNow,
            Today = DateTime.Today
        }).SingleAsync();

        Assert.Equal(new DateTime(2027, 8, 4, 13, 14, 15), result.PlusYear);
        Assert.Equal(new DateTime(2026, 9, 4, 13, 14, 15), result.PlusMonth);
        Assert.Equal(new DateTime(2026, 8, 5, 13, 14, 15), result.PlusDay);
        Assert.Equal(new DateTime(2026, 8, 4, 15, 14, 15), result.PlusHour);
        Assert.Equal(new DateOnly(2026, 9, 6), result.DateOnlyPlus);
        Assert.Equal(new TimeOnly(15, 29, 15), result.TimeOnlyPlus);
        Assert.Equal(new DateTime(2026, 8, 4, 13, 14, 15), result.Combined);
        Assert.Equal(new DateOnly(2026, 8, 4), result.DateFromTimestamp);
        Assert.Equal(new TimeOnly(13, 14, 15), result.TimeFromTimestamp);
        Assert.NotEqual(default, result.LocalNow);
        Assert.NotEqual(default, result.UtcNow);
        Assert.Equal(result.LocalNow.Date, result.Today);
    }

    [Fact]
    public async Task Lateral_apply_shapes_execute_on_kingbase()
    {
        await using var fixture = await QueryFixture.CreateAsync();
        if (fixture is null)
        {
            return;
        }

        await using var context = fixture.CreateContext();
        var inner = await (
            from parent in context.Rows.Where(row => row.Id == 1)
            from child in context.Rows
                .Where(row => row.ParentId == parent.Id)
                .OrderBy(row => row.Id)
                .Take(1)
            select child.Id).SingleAsync();
        Assert.Equal(2, inner);

        var outer = await (
            from parent in context.Rows.Where(row => row.Id == 3)
            from child in context.Rows
                .Where(row => row.ParentId == parent.Id)
                .OrderBy(row => row.Id)
                .Take(1)
                .DefaultIfEmpty()
            select (int?)child.Id).SingleAsync();
        Assert.Null(outer);
    }

    [Fact]
    public async Task Aggregate_and_element_operators_execute_on_kingbase()
    {
        await using var fixture = await QueryFixture.CreateAsync();
        if (fixture is null)
        {
            return;
        }

        await using var context = fixture.CreateContext();
        Assert.True(await context.Rows.AllAsync(row => row.Id > 0));
        Assert.True(await context.Rows.AnyAsync(row => row.Text.Contains("Beta")));
        Assert.Equal(3, await context.Rows.CountAsync());
        Assert.Equal(3L, await context.Rows.LongCountAsync());
        Assert.Equal(4.083333333333333, await context.Rows.AverageAsync(row => row.Number), 8);
        Assert.Equal(12.25, await context.Rows.SumAsync(row => row.Number), 8);
        Assert.Equal(-12.75, await context.Rows.MinAsync(row => row.Number), 8);
        Assert.Equal(20, await context.Rows.MaxAsync(row => row.Number), 8);
        Assert.Equal(1, (await context.Rows.OrderBy(row => row.Id).FirstAsync()).Id);
        Assert.Equal(3, (await context.Rows.OrderBy(row => row.Id).LastAsync()).Id);
        Assert.Equal(2, (await context.Rows.OrderBy(row => row.Id).ElementAtAsync(1)).Id);
        Assert.Null(await context.Rows.Where(row => row.Id < 0).SingleOrDefaultAsync());
    }

    [Fact]
    public async Task Set_ordering_and_paging_operators_execute_on_kingbase()
    {
        await using var fixture = await QueryFixture.CreateAsync();
        if (fixture is null)
        {
            return;
        }

        await using var context = fixture.CreateContext();
        var first = context.Rows.Where(row => row.Id <= 2).Select(row => row.Id);
        var second = context.Rows.Where(row => row.Id >= 2).Select(row => row.Id);

        Assert.Equal([1, 2, 2, 3], await first.Concat(second).OrderBy(value => value).ToListAsync());
        Assert.Equal([1, 2, 3], await first.Union(second).OrderBy(value => value).ToListAsync());
        Assert.Equal([2], await first.Intersect(second).ToListAsync());
        Assert.Equal([1], await first.Except(second).ToListAsync());
        Assert.Equal([1, 2, 3], await first.Concat(second).Distinct().OrderBy(value => value).ToListAsync());
        Assert.True(await first.ContainsAsync(2));
        Assert.Equal([2, 3], await context.Rows.OrderBy(row => row.Id).Skip(1).Take(2).Select(row => row.Id).ToListAsync());
        Assert.Equal([3, 2, 1], await context.Rows.OrderBy(row => row.Id).Reverse().Select(row => row.Id).ToListAsync());
        Assert.Equal([1, 2, 3], await context.Rows.Select(row => row.Id).Order().ToListAsync());
        Assert.Equal([3, 2, 1], await context.Rows.Select(row => row.Id).OrderDescending().ToListAsync());
        Assert.Equal(
            [1, 3, 2],
            await context.Rows
                .OrderBy(row => row.ParentId.HasValue)
                .ThenByDescending(row => row.Text)
                .Select(row => row.Id)
                .ToListAsync());
        Assert.Equal(
            [1, 2, 3],
            await context.Rows
                .OrderBy(row => row.ParentId.HasValue)
                .ThenBy(row => row.Id)
                .Select(row => row.Id)
                .ToListAsync());
    }

    [Fact]
    public async Task Grouping_and_join_operators_execute_on_kingbase()
    {
        await using var fixture = await QueryFixture.CreateAsync();
        if (fixture is null)
        {
            return;
        }

        await using var context = fixture.CreateContext();
        var grouped = await context.Rows
            .GroupBy(row => row.Id % 2)
            .Select(group => new { Key = group.Key, Count = group.Count(), Sum = group.Sum(row => row.Number) })
            .OrderBy(result => result.Key)
            .ToListAsync();

        Assert.Equal(2, grouped.Count);
        Assert.Equal(1, grouped[0].Count);
        Assert.Equal(2, grouped[1].Count);

        var joined = await context.Rows
            .Join(context.Rows, left => left.Id, right => right.Id, (left, right) => left.Text + right.Text)
            .ToListAsync();
        Assert.Equal(3, joined.Count);

        var leftJoinQuery = context.Rows
            .LeftJoin(
                context.Rows.Where(row => row.Id == 2),
                left => left.Id,
                right => right.Id,
                (left, right) => new { left.Id, RightId = right == null ? (int?)null : right.Id })
            .OrderBy(row => row.Id);
        var leftJoined = await leftJoinQuery.ToListAsync();
        Assert.True(
            new int?[] { null, 2, null }.SequenceEqual(leftJoined.Select(row => row.RightId)),
            leftJoinQuery.ToQueryString());

        var rightJoinQuery = context.Rows.Where(row => row.Id == 2)
            .RightJoin(
                context.Rows,
                left => left.Id,
                right => right.Id,
                (left, right) => new { LeftId = left == null ? (int?)null : left.Id, right.Id })
            .OrderBy(row => row.Id);
        var rightJoined = await rightJoinQuery.ToListAsync();
        Assert.True(
            new int?[] { null, 2, null }.SequenceEqual(rightJoined.Select(row => row.LeftId)),
            rightJoinQuery.ToQueryString());
    }

    [Fact]
    public async Task Remaining_relational_queryable_shapes_execute_on_kingbase()
    {
        await using var fixture = await QueryFixture.CreateAsync();
        if (fixture is null)
        {
            return;
        }

        await using var context = fixture.CreateContext();

        Assert.Equal(0, await context.Rows.Where(row => row.Id < 0).Select(row => row.Id).DefaultIfEmpty().SingleAsync());
        Assert.Equal(9, await context.Rows.SelectMany(_ => context.Rows).CountAsync());
        Assert.Equal([3, 2, 1], await context.Rows.OrderByDescending(row => row.Id).Select(row => row.Id).ToListAsync());
        Assert.Equal(3, await context.Rows.OfType<QueryRow>().CountAsync());

        var castValues = await context.Rows
            .Select(row => (object)row.Id)
            .Cast<int>()
            .OrderBy(value => value)
            .ToListAsync();
        Assert.Equal([1, 2, 3], castValues);

        var groupedJoin = await context.Rows
            .GroupJoin(
                context.Rows,
                outer => outer.Id,
                inner => inner.ParentId,
                (outer, children) => new { outer.Id, Count = children.Count() })
            .OrderBy(row => row.Id)
            .ToListAsync();
        Assert.Equal([2, 0, 0], groupedJoin.Select(row => row.Count).ToArray());
    }

    [Fact]
    public async Task Ef_query_extensions_execute_on_kingbase()
    {
        await using var fixture = await QueryFixture.CreateAsync();
        if (fixture is null)
        {
            return;
        }

        await using var context = fixture.CreateContext();

        var trackingQuery = context.Rows
            .TagWith("ef-query-extension-matrix")
            .TagWithCallSite()
            .IgnoreAutoIncludes()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AsNoTrackingWithIdentityResolution()
            .AsTracking();

        Assert.Contains("ef-query-extension-matrix", trackingQuery.ToQueryString());
        Assert.Equal(3, (await trackingQuery.ToArrayAsync()).Length);
        Assert.Equal(3, (await trackingQuery.ToDictionaryAsync(row => row.Id)).Count);
        Assert.Equal(3, (await trackingQuery.Select(row => row.Id).ToHashSetAsync()).Count);

        var forEachCount = 0;
        await trackingQuery.ForEachAsync(_ => forEachCount++);
        Assert.Equal(3, forEachCount);

        await context.Rows.Where(row => row.Id == 1).LoadAsync();
        context.Rows.Where(row => row.Id == 2).Load();
        Assert.True(context.ChangeTracker.Entries<QueryRow>().Count() >= 2);

        var asyncIds = new List<int>();
        await foreach (var row in context.Rows.OrderBy(row => row.Id).AsAsyncEnumerable())
        {
            asyncIds.Add(row.Id);
        }
        Assert.Equal([1, 2, 3], asyncIds);

        Assert.Equal(1, (await context.Rows.FirstOrDefaultAsync(row => row.Id == 1))?.Id);
        Assert.Equal(3, (await context.Rows.OrderBy(row => row.Id).LastOrDefaultAsync())?.Id);
        Assert.Equal(2, (await context.Rows.OrderBy(row => row.Id).ElementAtOrDefaultAsync(1))?.Id);

        Assert.Equal(1, await context.Rows.Where(row => row.Id == 2)
            .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Text, row => row.Text + "!")));
        Assert.Equal(1, context.Rows.Where(row => row.Id == 2)
            .ExecuteUpdate(setters => setters.SetProperty(row => row.Number, row => row.Number + 1)));
        Assert.Equal("Beta!", await context.Rows.Where(row => row.Id == 2).Select(row => row.Text).SingleAsync());

        var includeQuery = context.Rows
            .Where(row => row.Id == 1)
            .Include(row => row.Children)
            .ThenInclude(row => row.Children)
            .AsSplitQuery();
        var parent = await includeQuery.SingleAsync();
        Assert.Equal(2, parent.Children.Count);
        Assert.All(parent.Children, child => Assert.Same(parent, child.Parent));
        Assert.All(parent.Children, child => Assert.Empty(child.Children));
        Assert.Single(await includeQuery.AsSingleQuery().ToListAsync());
    }

    [Fact]
    public async Task Relational_query_extensions_execute_on_kingbase()
    {
        await using var fixture = await QueryFixture.CreateAsync();
        if (fixture is null)
        {
            return;
        }

        await using var context = fixture.CreateContext();
        Assert.Equal(3, await context.Rows
            .FromSqlRaw("SELECT \"Id\", \"Text\", \"OccurredAt\", \"Number\", \"ParentId\" FROM \"efcore_query_translation_probe\"")
            .CountAsync());
        Assert.Equal(3, await context.Rows
            .FromSqlInterpolated($"SELECT \"Id\", \"Text\", \"OccurredAt\", \"Number\", \"ParentId\" FROM \"efcore_query_translation_probe\"")
            .CountAsync());
        Assert.Equal(3, await context.Rows
            .FromSql($"SELECT \"Id\", \"Text\", \"OccurredAt\", \"Number\", \"ParentId\" FROM \"efcore_query_translation_probe\"")
            .CountAsync());

        await using DbCommand command = context.Rows.Where(row => row.Id == 1).CreateDbCommand();
        Assert.IsType<KdbndpCommand>(command);
        Assert.Contains("efcore_query_translation_probe", command.CommandText);
    }

    private sealed class QueryFixture : IAsyncDisposable
    {
        private readonly KdbndpConnection _connection;
        private readonly DbContextOptions<QueryDbContext> _options;

        private QueryFixture(KdbndpConnection connection)
        {
            _connection = connection;
            _options = new DbContextOptionsBuilder<QueryDbContext>()
                .UseKdbndp(connection, kingbaseOptionsAction: options => options.SetOracleCompatibilityMode())
                .Options;
        }

        public static async Task<QueryFixture?> CreateAsync()
        {
            var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return null;
            }

            var connection = new KdbndpConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                DROP TABLE IF EXISTS "efcore_query_translation_probe";
                CREATE TABLE "efcore_query_translation_probe" (
                    "Id" integer NOT NULL PRIMARY KEY,
                    "Text" text NOT NULL,
                    "OccurredAt" timestamp without time zone NOT NULL,
                    "Number" double precision NOT NULL,
                    "Amount" numeric(18,4) NOT NULL,
                    "NullableNumber" integer NULL,
                    "Token" uuid NOT NULL,
                    "Bytes" bytea NOT NULL,
                    "BytesCopy" bytea NOT NULL,
                    "EventDate" date NOT NULL,
                    "EventTime" time without time zone NOT NULL,
                    "Flags" integer NOT NULL,
                    "ParentId" integer NULL
                );
                INSERT INTO "efcore_query_translation_probe" ("Id", "Text", "OccurredAt", "Number", "Amount", "NullableNumber", "Token", "Bytes", "BytesCopy", "EventDate", "EventTime", "Flags", "ParentId") VALUES
                    (1, ' Alpha ', TIMESTAMP '2026-08-04 13:14:15', -12.75, 10.7500, NULL, CAST('11111111-1111-1111-1111-111111111111' AS uuid), '\x010203', '\x010203', DATE '2026-08-04', TIME '13:14:15', 3, NULL),
                    (2, 'Beta', TIMESTAMP '2026-08-05 14:15:16', 5.0, 5.0000, 7, CAST('22222222-2222-2222-2222-222222222222' AS uuid), '\x0405', '\x0405', DATE '2026-08-05', TIME '14:15:16', 1, 1),
                    (3, 'Gamma', TIMESTAMP '2026-08-06 15:16:17', 20.0, 20.0000, 8, CAST('33333333-3333-3333-3333-333333333333' AS uuid), '\x06', '\x06', DATE '2026-08-06', TIME '15:16:17', 0, 1);
                """;
            await command.ExecuteNonQueryAsync();
            return new QueryFixture(connection);
        }

        public QueryDbContext CreateContext()
            => new(_options);

        public async ValueTask DisposeAsync()
        {
            await using var command = _connection.CreateCommand();
            command.CommandText = "DROP TABLE IF EXISTS \"efcore_query_translation_probe\";";
            await command.ExecuteNonQueryAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class QueryDbContext(DbContextOptions<QueryDbContext> options) : DbContext(options)
    {
        public DbSet<QueryRow> Rows => Set<QueryRow>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<QueryRow>(entity =>
            {
                entity.ToTable("efcore_query_translation_probe");
                entity.HasKey(row => row.Id);
                entity.Property(row => row.Id).ValueGeneratedNever();
                entity.HasOne(row => row.Parent)
                    .WithMany(row => row.Children)
                    .HasForeignKey(row => row.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }

    private sealed class QueryRow
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public double Number { get; set; }
        public decimal Amount { get; set; }
        public int? NullableNumber { get; set; }
        public Guid Token { get; set; }
        public byte[] Bytes { get; set; } = [];
        public byte[] BytesCopy { get; set; } = [];
        public DateOnly EventDate { get; set; }
        public TimeOnly EventTime { get; set; }
        public QueryFlags Flags { get; set; }
        public int? ParentId { get; set; }
        public QueryRow? Parent { get; set; }
        public List<QueryRow> Children { get; set; } = [];
    }

    [Flags]
    private enum QueryFlags
    {
        None = 0,
        Read = 1,
        Write = 2
    }
}
