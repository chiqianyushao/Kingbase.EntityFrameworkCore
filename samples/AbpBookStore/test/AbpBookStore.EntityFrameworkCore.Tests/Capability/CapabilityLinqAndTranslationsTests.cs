using AbpBookStore.Authors;
using AbpBookStore.Books;
using Kdbndp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace AbpBookStore.EntityFrameworkCore.Tests.Capability;

/// <summary>
/// Re-verifies the report's §5 (Queryable operators), §6 (query extensions) and
/// §7 (expression/function translation) THROUGH the real ABP app stack against a
/// real KingbaseES database. The provider's own suite already covers these at the
/// provider level; these tests prove the same translations survive the ABP
/// DbContext + repository + UoW layer with the ABP conventions model.
/// </summary>
public sealed class CapabilityLinqAndTranslationsTests : CapabilityTestBase
{
    public CapabilityLinqAndTranslationsTests(AbpAppFixture fixture, ITestOutputHelper output)
        : base(fixture, output)
    {
    }

    private static Book Book1984() => NewBook("1984", BookType.Dystopia, new DateTime(1949, 6, 8), 19.84f);
    private static Book BookAnimalFarm() => NewBook("Animal Farm", BookType.Poetry, new DateTime(1945, 8, 17), 9.99f);
    private static Book BookHitchhiker() => NewBook("The Hitchhiker's Guide to the Galaxy", BookType.ScienceFiction, new DateTime(1979, 10, 12), 42.0f);
    private static Book BookBraveNewWorld() => NewBook("Brave New World", BookType.Dystopia, new DateTime(1932, 1, 1), 15.0f);

    [Fact]
    public async Task Comparison_logic_arithmetic_and_ternary_execute_with_dotnet_semantics()
    {
        if (!HasDatabase) return;
        await ResetSchemaAsync();
        // Use exactly-representable float32 prices (10/20/40) so the real-column
        // comparisons are exact — 9.99 is not representable in `real` and a
        // boundary comparison would silently exclude the boundary row.
        await SeedBooksAsync(
            NewBook("Compare A", BookType.Adventure, new DateTime(2001, 1, 1), 10),
            NewBook("Compare B", BookType.Biography, new DateTime(2002, 1, 1), 20),
            NewBook("Compare C", BookType.Dystopia, new DateTime(2003, 1, 1), 40));

        await InDbContextAsync(async context =>
        {
            // == / != / > / >= / < / <=
            Assert.Equal(1, await context.Books.CountAsync(b => b.Name == "Compare A"));
            Assert.Equal(2, await context.Books.CountAsync(b => b.Name != "Compare A"));
            Assert.Equal(1, await context.Books.CountAsync(b => b.Price > 30));
            Assert.Equal(3, await context.Books.CountAsync(b => b.Price >= 10));
            Assert.Equal(1, await context.Books.CountAsync(b => b.Price < 15));
            Assert.Equal(2, await context.Books.CountAsync(b => b.Price <= 20));

            // && || !
            Assert.Equal(1, await context.Books.CountAsync(b => b.Price > 10 && b.Price < 30));
            Assert.Equal(2, await context.Books.CountAsync(b => b.Price < 15 || b.Price > 25)); // 10 and 40
            Assert.Equal(1, await context.Books.CountAsync(b => !(b.Price > 10)));

            // + - * / % (decimal/float arithmetic in projection)
            // NOTE: real % integer has no operator in KingbaseES (42883). The
            // report §7 only claims integer modulo, so the modulo operand is
            // cast to integer to stay within the verified surface.
            var arithmetic = await context.Books
                .Where(b => b.Name == "Compare B")
                .Select(b => new
                {
                    Plus = b.Price + 2,
                    Minus = b.Price - 2,
                    Times = b.Price * 2,
                    Div = b.Price / 2,
                    Mod = (int)b.Price % 3
                })
                .SingleAsync();

            Assert.Equal(22.0f, arithmetic.Plus, 0.001);
            Assert.Equal(18.0f, arithmetic.Minus, 0.001);
            Assert.Equal(40.0f, arithmetic.Times, 0.001);
            Assert.Equal(10.0f, arithmetic.Div, 0.001);
            Assert.Equal(2.0f, arithmetic.Mod, 0.001);

            // Ternary ?:  ->  CASE
            var ternary = await context.Books
                .Select(b => b.Price > 20 ? "expensive" : "cheap")
                .OrderBy(x => x)
                .ToListAsync();
            Assert.Equal(["cheap", "cheap", "expensive"], ternary);

            // string concatenation -> ||
            var concat = await context.Books
                .Where(b => b.Name == "Compare B")
                .Select(b => b.Name + " - book")
                .SingleAsync();
            Assert.Equal("Compare B - book", concat);

            // string.Equals
            Assert.Equal(1, await context.Books.CountAsync(b => b.Name.Equals("Compare A")));
            return 0;
        });
    }

    [Fact]
    public async Task Null_coalesce_executes_with_coalesce()
    {
        if (!HasDatabase) return;
        await ResetSchemaAsync();
        var authorWithBio = NewAuthor("Bio Author", new DateTime(1970, 1, 1));
        authorWithBio.ShortBio = "hello";
        await SeedAuthorsAsync(authorWithBio, NewAuthor("No Bio Author", new DateTime(1971, 1, 1)));

        await InDbContextAsync(async context =>
        {
            var result = await context.Authors
                .OrderBy(a => a.Name)
                .Select(a => a.ShortBio ?? "fallback")
                .ToListAsync();

            // "Bio Author" < "No Bio Author" alphabetically -> ["hello", "fallback"]
            Assert.Equal(["hello", "fallback"], result);
            return 0;
        });
    }

    [Fact]
    public async Task String_translations_execute_with_dotnet_semantics()
    {
        if (!HasDatabase) return;
        await ResetSchemaAsync();
        var books = new[] { Book1984(), BookAnimalFarm() }; // "1984", "Animal Farm"
        await SeedBooksAsync(books);

        await InDbContextAsync(async context =>
        {
            Assert.Equal(1, await context.Books.CountAsync(b => b.Name.Contains("i")));       // strpos != 0
            Assert.Equal(1, await context.Books.CountAsync(b => b.Name.Contains("1984")));
            Assert.Equal(0, await context.Books.CountAsync(b => b.Name.Contains("i9")));
            Assert.Equal(1, await context.Books.CountAsync(b => b.Name.StartsWith("A")));    // left
            Assert.Equal(1, await context.Books.CountAsync(b => b.Name.EndsWith("rm")));     // right
            Assert.Equal(1, await context.Books.CountAsync(b => b.Name.ToLower() == "1984")); // lower
            Assert.Equal(1, await context.Books.CountAsync(b => b.Name.ToUpper() == "1984")); // upper
            Assert.Equal(1, await context.Books.CountAsync(b => b.Name.Length == 4));        // length

            // .NET zero-based semantics: "1984" -> Sub "198", IndexOf("i") = -1, Replace keeps "1984"
            var rows = await context.Books
                .OrderBy(b => b.Name)
                .Select(b => new
                {
                    b.Name,
                    Sub = b.Name.Substring(0, 3),
                    IndexOf = b.Name.IndexOf("i"),
                    Replace = b.Name.Replace("i", "I"),
                    Trimmed = b.Name.Trim()
                })
                .ToListAsync();

            Assert.Equal("1984", rows[0].Name);
            Assert.Equal("198", rows[0].Sub);
            Assert.Equal(-1, rows[0].IndexOf);
            Assert.Equal("1984", rows[0].Replace);
            Assert.Equal("1984", rows[0].Trimmed);

            Assert.Equal("Animal Farm", rows[1].Name);
            Assert.Equal("Ani", rows[1].Sub);
            Assert.Equal(2, rows[1].IndexOf);   // 'A'=0,'n'=1,'i'=2
            Assert.Equal("AnImal Farm", rows[1].Replace);
            Assert.Equal("Animal Farm", rows[1].Trimmed);
            return 0;
        });
    }

    [Fact]
    public async Task Date_translations_execute_on_kingbase()
    {
        if (!HasDatabase) return;
        await ResetSchemaAsync();
        await SeedBooksAsync(Book1984());

        await InDbContextAsync(async context =>
        {
            var parts = await context.Books
                .Where(b => b.Name == "1984")
                .Select(b => new
                {
                    b.PublishDate.Year,
                    b.PublishDate.Month,
                    b.PublishDate.Day,
                    b.PublishDate.Hour,
                    b.PublishDate.Minute,
                    b.PublishDate.Second,
                    AddYears = b.PublishDate.AddYears(1),
                    AddMonths = b.PublishDate.AddMonths(2),
                    AddDays = b.PublishDate.AddDays(3),
                    AddHours = b.PublishDate.AddHours(5)
                })
                .SingleAsync();

            Assert.Equal(1949, parts.Year);
            Assert.Equal(6, parts.Month);
            Assert.Equal(8, parts.Day);
            Assert.Equal(0, parts.Hour);
            Assert.Equal(0, parts.Minute);
            Assert.Equal(0, parts.Second);
            Assert.Equal(new DateTime(1950, 6, 8), parts.AddYears);
            Assert.Equal(new DateTime(1949, 8, 8), parts.AddMonths);
            Assert.Equal(new DateTime(1949, 6, 11), parts.AddDays);
            Assert.Equal(new DateTime(1949, 6, 8, 5, 0, 0), parts.AddHours);

            Assert.Equal(1, await context.Books.CountAsync(b => b.PublishDate.Year == 1949));
            return 0;
        });
    }

    [Fact]
    public async Task Math_translations_execute_on_kingbase()
    {
        if (!HasDatabase) return;
        await ResetSchemaAsync();
        await SeedBooksAsync(BookAnimalFarm()); // Price = 9.99

        await InDbContextAsync(async context =>
        {
            var math = await context.Books
                .Where(b => b.Name == "Animal Farm")
                .Select(b => new
                {
                    Abs = Math.Abs(b.Price),
                    Ceiling = Math.Ceiling((double)b.Price),
                    Floor = Math.Floor((double)b.Price),
                    Round = Math.Round((double)b.Price),
                    Sqrt = Math.Sqrt((double)b.Price),
                    Cbrt = Math.Cbrt(27.0)
                })
                .SingleAsync();

            Assert.Equal(9.99, math.Abs, 0.001);
            Assert.Equal(10, math.Ceiling);
            Assert.Equal(9, math.Floor);
            Assert.Equal(10, math.Round);
            Assert.Equal(Math.Sqrt(9.99), math.Sqrt, 0.001);
            Assert.Equal(3.0, math.Cbrt, 0.001);
            return 0;
        });
    }

    [Fact]
    public async Task Aggregate_operators_execute_on_kingbase()
    {
        if (!HasDatabase) return;
        await ResetSchemaAsync();
        var books = new[] { Book1984(), BookAnimalFarm(), BookHitchhiker() };
        await SeedBooksAsync(books);

        await InDbContextAsync(async context =>
        {
            Assert.True(await context.Books.AllAsync(b => b.Price > 0));
            Assert.True(await context.Books.AnyAsync(b => b.Price > 40));
            Assert.False(await context.Books.AnyAsync(b => b.Price > 100));
            Assert.Equal(3, await context.Books.CountAsync());
            Assert.Equal(3L, await context.Books.LongCountAsync());
            Assert.Equal(71.83f, await context.Books.SumAsync(b => b.Price), 0.01);
            Assert.Equal(9.99f, await context.Books.MinAsync(b => b.Price));
            Assert.Equal(42.0f, await context.Books.MaxAsync(b => b.Price));
            Assert.Equal(71.83f / 3, await context.Books.AverageAsync(b => b.Price), 0.01);
            return 0;
        });
    }

    [Fact]
    public async Task Element_operators_execute_on_kingbase()
    {
        if (!HasDatabase) return;
        await ResetSchemaAsync();
        var books = new[] { BookAnimalFarm(), Book1984(), BookHitchhiker() }; // prices 9.99, 19.84, 42
        await SeedBooksAsync(books);

        await InDbContextAsync(async context =>
        {
            var ordered = context.Books.OrderBy(b => b.Price);
            Assert.Equal("Animal Farm", (await ordered.FirstAsync()).Name);
            Assert.Equal("Animal Farm", (await ordered.FirstOrDefaultAsync())!.Name);
            Assert.Equal("Animal Farm", (await ordered.SingleAsync(b => b.Price < 10)).Name);
            Assert.Null(await ordered.FirstOrDefaultAsync(b => b.Price < 0));
            Assert.Equal("The Hitchhiker's Guide to the Galaxy", (await ordered.LastAsync()).Name);
            Assert.Equal("1984", (await ordered.ElementAtAsync(1)).Name);
            Assert.Null(await ordered.ElementAtOrDefaultAsync(99));
            return 0;
        });
    }

    [Fact]
    public async Task Set_ordering_and_paging_operators_execute_on_kingbase()
    {
        if (!HasDatabase) return;
        await ResetSchemaAsync();
        var books = new[]
        {
            Book1984(),                       // Dystopia 19.84
            BookAnimalFarm(),                 // Poetry 9.99
            BookHitchhiker(),                 // ScienceFiction 42
            NewBook("1984 Duplicate", BookType.Dystopia, new DateTime(1949, 6, 8), 1.0f)
        };
        await SeedBooksAsync(books);

        await InDbContextAsync(async context =>
        {
            // Distinct on a scalar projection
            var types = await context.Books.Select(b => b.Type).Distinct().ToListAsync();
            Assert.Equal(3, types.Count);
            Assert.Contains(BookType.Dystopia, types);
            Assert.Contains(BookType.Poetry, types);
            Assert.Contains(BookType.ScienceFiction, types);

            // OrderBy / OrderByDescending / ThenBy / ThenByDescending / Reverse
            var ordered = await context.Books
                .OrderBy(b => b.Type)
                .ThenByDescending(b => b.Price)
                .Select(b => b.Price)
                .ToListAsync();
            Assert.Equal([19.84f, 1.0f, 42.0f, 9.99f], ordered);

            var reversed = await context.Books
                .OrderBy(b => b.Price)
                .Select(b => b.Price)
                .Reverse()
                .ToListAsync();
            Assert.Equal([42.0f, 19.84f, 9.99f, 1.0f], reversed);

            // Skip / Take (OFFSET + LIMIT)
            var page = await context.Books.OrderBy(b => b.Price).Select(b => b.Price).Skip(1).Take(2).ToListAsync();
            Assert.Equal([9.99f, 19.84f], page);

            // Concat / Union / Intersect / Except / Contains
            // Dystopia prices: {19.84, 1.0};  prices < 10: {9.99, 1.0}
            var first = context.Books.Where(b => b.Type == BookType.Dystopia).Select(b => b.Price);
            var second = context.Books.Where(b => b.Price < 10).Select(b => b.Price);
            Assert.Equal(4, (await first.Concat(second).ToListAsync()).Count);
            Assert.Equal(3, (await first.Union(second).ToListAsync()).Count);  // {19.84, 1.0, 9.99}
            Assert.Equal(1, (await first.Intersect(second).ToListAsync()).Count); // {1.0}
            Assert.Equal(1, (await first.Except(second).ToListAsync()).Count);    // {19.84}
            Assert.Contains(9.99f, await second.ToListAsync());
            return 0;
        });
    }

    [Fact]
    public async Task Join_grouping_and_select_many_execute_on_kingbase()
    {
        if (!HasDatabase) return;
        await ResetSchemaAsync();
        var orwell = NewAuthor("George Orwell", new DateTime(1903, 6, 25));
        var huxley = NewAuthor("Aldous Huxley", new DateTime(1894, 7, 26));
        await SeedAuthorsAsync(orwell, huxley);

        var book1984 = Book1984();                 // Dystopia
        book1984.BookAuthors.Add(new BookAuthor { AuthorId = orwell.Id });
        var hitchhiker = BookHitchhiker();         // ScienceFiction
        hitchhiker.BookAuthors.Add(new BookAuthor { AuthorId = huxley.Id });
        await SeedBooksAsync(book1984, hitchhiker);

        await InDbContextAsync(async context =>
        {
            // Join
            var join = await context.Books
                .Join(context.BookAuthors, b => b.Id, ba => ba.BookId, (b, ba) => ba.AuthorId)
                .ToListAsync();
            Assert.Equal(2, join.Count);
            Assert.Contains(orwell.Id, join);
            Assert.Contains(huxley.Id, join);

            // GroupJoin + Count
            var groupJoin = await context.Authors
                .GroupJoin(context.BookAuthors, a => a.Id, ba => ba.AuthorId, (a, links) => new { a.Name, Count = links.Count() })
                .ToListAsync();
            Assert.Equal(2, groupJoin.Count);
            Assert.All(groupJoin, x => Assert.Equal(1, x.Count));

            // GroupBy + Count / Sum
            var group = await context.Books
                .GroupBy(b => b.Type)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync();
            Assert.Equal(2, group.Count);
            Assert.Contains(group, g => g.Key == BookType.Dystopia && g.Count == 1);

            // SelectMany
            var authors = await context.Books
                .SelectMany(b => b.BookAuthors, (b, ba) => ba.Author.Name)
                .OrderBy(x => x)
                .ToListAsync();
            Assert.Equal(["Aldous Huxley", "George Orwell"], authors);
            return 0;
        });
    }

    [Fact]
    public async Task Ef_functions_guid_enum_execute_on_kingbase()
    {
        if (!HasDatabase) return;
        await ResetSchemaAsync();
        await SeedBooksAsync(Book1984(), BookAnimalFarm());

        await InDbContextAsync(async context =>
        {
            // EF.Functions.Like
            Assert.Equal(1, await context.Books.CountAsync(b => EF.Functions.Like(b.Name, "198%")));
            // EF.Functions.Random()
            Assert.Equal(2, await context.Books.CountAsync(b => EF.Functions.Random() >= 0));
            // EF.Functions.Collate (compare against a value, not "" — Oracle mode
            // treats the empty string as NULL so `!= ""` never matches)
            Assert.Equal(1, await context.Books.CountAsync(b => EF.Functions.Collate(b.Name, "c") == "1984"));

            // Guid.NewGuid()
            var guids = await context.Books.Select(b => Guid.NewGuid()).ToListAsync();
            Assert.Equal(2, guids.Count);
            Assert.All(guids, g => Assert.NotEqual(Guid.Empty, g));

            // Enum HasFlag / ToString
            Assert.Equal(1, await context.Books.CountAsync(b => b.Type.HasFlag(BookType.Dystopia)));
            var typeName = await context.Books.Where(b => b.Name == "1984").Select(b => b.Type.ToString()).SingleAsync();
            Assert.Equal("Dystopia", typeName);
            return 0;
        });
    }

    [Fact]
    public async Task Query_extensions_execute_on_kingbase()
    {
        if (!HasDatabase) return;
        await ResetSchemaAsync();
        var orwell = NewAuthor("George Orwell", new DateTime(1903, 6, 25));
        await SeedAuthorsAsync(orwell);
        var book = Book1984();
        book.BookAuthors.Add(new BookAuthor { AuthorId = orwell.Id });
        await SeedBooksAsync(book);

        await InDbContextAsync(async context =>
        {
            // AsNoTracking / AsNoTrackingWithIdentityResolution / AsTracking
            var noTrack = await context.Books.AsNoTracking().ToListAsync();
            Assert.Equal(1, noTrack.Count);
            var noTrackId = await context.Books.AsNoTrackingWithIdentityResolution().ToListAsync();
            Assert.Equal(1, noTrackId.Count);
            var track = await context.Books.AsTracking().ToListAsync();
            Assert.Equal(1, track.Count);

            // TagWith / TagWithCallSite
            var tagged = await context.Books.TagWith("capability-tag").ToListAsync();
            Assert.Equal(1, tagged.Count);
            var withCallSite = await context.Books.TagWithCallSite().Select(b => b.Name).ToListAsync();
            Assert.Single(withCallSite);

            // Include / ThenInclude
            var included = await context.Books
                .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
                .SingleAsync();
            Assert.Single(included.BookAuthors);
            Assert.Equal("George Orwell", included.BookAuthors.Single().Author.Name);

            // AsSingleQuery / AsSplitQuery
            var single = await context.Books.AsSingleQuery().Include(b => b.BookAuthors).ToListAsync();
            Assert.Single(single);
            var split = await context.Books.AsSplitQuery().Include(b => b.BookAuthors).ToListAsync();
            Assert.Single(split);

            // Load / LoadAsync / ForEachAsync
            var books = await context.Books.ToListAsync();
            context.Books.IgnoreQueryFilters().Load();
            await context.Books.IgnoreQueryFilters().LoadAsync();
            var names = new List<string>();
            await context.Books.OrderBy(b => b.Name).Select(b => b.Name).ForEachAsync(names.Add);
            Assert.Equal(1, names.Count);

            // ToQueryString
            var sql = context.Books.Where(b => b.Price > 5).TagWith("cap-tag").ToQueryString();
            Assert.Contains("cap-tag", sql);
            Assert.Contains("SELECT", sql);

            // AsAsyncEnumerable + ToDictionaryAsync / ToHashSetAsync / ToArrayAsync
            var dict = await context.Books.ToDictionaryAsync(b => b.Name, b => b.Price);
            Assert.Equal(1, dict.Count);
            var set = await context.Books.Select(b => b.Name).ToHashSetAsync();
            Assert.Single(set);
            var arr = await context.Books.Select(b => b.Name).ToArrayAsync();
            Assert.Single(arr);
            await foreach (var b in context.Books.AsAsyncEnumerable())
            {
                Assert.NotNull(b.Name);
            }
            return 0;
        });
    }

    [Fact]
    public async Task From_sql_and_create_db_command_execute_on_kingbase()
    {
        if (!HasDatabase) return;
        await ResetSchemaAsync();
        await SeedBooksAsync(Book1984(), BookAnimalFarm());

        await InDbContextAsync(async context =>
        {
            // FromSqlRaw / FromSqlInterpolated / FromSql
            var raw = await context.Books
                .FromSqlRaw("SELECT * FROM \"Books\"")
                .OrderBy(b => b.Name)
                .ToListAsync();
            Assert.Equal(2, raw.Count);

            var interpolated = await context.Books
                .FromSqlInterpolated($"SELECT * FROM \"Books\" WHERE \"Price\" > {10.0f}")
                .ToListAsync();
            Assert.Single(interpolated);
            Assert.Equal("1984", interpolated[0].Name);

            var composed = await context.Books
                .FromSql($"SELECT * FROM \"Books\"")
                .Where(b => b.Price > 10)
                .ToListAsync();
            Assert.Single(composed);

            // CreateDbCommand returns a native KdbndpCommand
            await context.Database.OpenConnectionAsync();
            using (var command = context.Books.CreateDbCommand())
            {
                Assert.IsType<KdbndpCommand>(command);
                command.CommandText = "SELECT COUNT(*) FROM \"Books\"";
                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                Assert.Equal(2, count);
            }
            await context.Database.CloseConnectionAsync();
            return 0;
        });
    }

    [Fact]
    public async Task Ignore_query_filters_reveals_soft_deleted()
    {
        if (!HasDatabase) return;
        await ResetSchemaAsync();
        await SeedBooksAsync(Book1984());

        // Soft delete via the repository, then verify the query filter hides it and IgnoreQueryFilters reveals it.
        await InDbContextAsync(async context =>
        {
            var book = await context.Books.SingleAsync();
            context.Books.Remove(book);
            await context.SaveChangesAsync();
            return 0;
        });

        await InDbContextAsync(async context =>
        {
            Assert.Equal(0, await context.Books.CountAsync());
            Assert.Equal(1, await context.Books.IgnoreQueryFilters().CountAsync());
            return 0;
        });
    }
}
