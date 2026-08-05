using AbpBookStore.Authors;
using AbpBookStore.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Uow;

namespace AbpBookStore.EntityFrameworkCore.Tests;

/// <summary>
/// Exercises the real ABP stack (module bootstrap, IRepository, unit of work,
/// IDataSeeder, custom repository query pipeline) against a real KingbaseES
/// database through the Kingbase.EntityFrameworkCore provider.
/// </summary>
public sealed class ModuleAndRepositoryTests : AbpBookStoreTestBase
{
    public ModuleAndRepositoryTests(AbpAppFixture fixture, ITestOutputHelper output)
        : base(fixture, output)
    {
    }

    [Fact]
    public async Task Module_creates_schema_with_three_tables()
    {
        if (!HasDatabase)
        {
            return;
        }

        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);

        using var scope = CreateScope();
        using var uow = BeginUnitOfWork(scope);
        var context = await scope.ServiceProvider
            .GetRequiredService<IDbContextProvider<BookStoreDbContext>>()
            .GetDbContextAsync();

        var tables = new List<string>();
        await context.Database.OpenConnectionAsync();
        await using (var command = context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText =
                """
                SELECT c.relname
                FROM sys_class AS c
                JOIN sys_namespace AS n ON n.oid = c.relnamespace
                WHERE c.relkind = 'r' AND n.nspname = current_schema()
                  AND c.relname IN ('Books', 'Authors', 'BookAuthors')
                """;
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        await uow.CompleteAsync();

        Assert.Contains("Books", tables);
        Assert.Contains("Authors", tables);
        Assert.Contains("BookAuthors", tables);
    }

    [Fact]
    public async Task Seeder_seeds_authors_and_books_with_m2m_links()
    {
        if (!HasDatabase)
        {
            return;
        }

        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);

        using var scope = CreateScope();
        var dataSeeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        await dataSeeder.SeedAsync();

        using var uow = BeginUnitOfWork(scope);
        var bookRepository = scope.ServiceProvider.GetRequiredService<IRepository<Book, Guid>>();
        var authorRepository = scope.ServiceProvider.GetRequiredService<IRepository<Author, Guid>>();
        var context = await scope.ServiceProvider
            .GetRequiredService<IDbContextProvider<BookStoreDbContext>>()
            .GetDbContextAsync();

        Assert.Equal(3, await bookRepository.GetCountAsync());
        Assert.Equal(2, await authorRepository.GetCountAsync());
        Assert.Equal(3, await context.BookAuthors.CountAsync());

        var page = await scope.ServiceProvider
            .GetRequiredService<IBookRepository>()
            .GetPagedListAsync(0, 10, nameof(Book.Name));

        var book1984 = page.Single(b => b.Name == "1984");
        var author = Assert.Single(book1984.BookAuthors).Author;
        Assert.Equal("George Orwell", author.Name);

        await uow.CompleteAsync();
    }

    [Fact]
    public async Task Book_creation_and_readback_with_authors()
    {
        if (!HasDatabase)
        {
            return;
        }

        var bookId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);

        using (var scope = CreateScope())
        {
            using var uow = BeginUnitOfWork(scope);
            await scope.ServiceProvider
                .GetRequiredService<IRepository<Author, Guid>>()
                .InsertAsync(new Author(authorId)
                {
                    Name = "George Orwell",
                    BirthDate = new DateTime(1903, 6, 25)
                });

            var book = new Book(bookId)
            {
                Name = "1984",
                Type = BookType.Dystopia,
                PublishDate = new DateTime(1949, 6, 8),
                Price = 19.84f
            };
            book.BookAuthors.Add(new BookAuthor { AuthorId = authorId, Book = book });

            await scope.ServiceProvider
                .GetRequiredService<IRepository<Book, Guid>>()
                .InsertAsync(book);

            await uow.CompleteAsync();
        }

        using (var scope = CreateScope())
        {
            using var uow = BeginUnitOfWork(scope);
            var list = await scope.ServiceProvider
                .GetRequiredService<IBookRepository>()
                .GetPagedListAsync(0, 10, nameof(Book.Name));

            var loaded = list.Single(b => b.Id == bookId);
            Assert.Equal("1984", loaded.Name);
            Assert.Equal(BookType.Dystopia, loaded.Type);
            Assert.Equal(19.84f, loaded.Price);
            Assert.Equal("George Orwell", Assert.Single(loaded.BookAuthors).Author.Name);

            await uow.CompleteAsync();
        }
    }

    [Fact]
    public async Task GetPagedList_filters_sorts_and_pages()
    {
        if (!HasDatabase)
        {
            return;
        }

        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);

        using (var scope = CreateScope())
        {
            using var uow = BeginUnitOfWork(scope);
            var bookRepository = scope.ServiceProvider.GetRequiredService<IRepository<Book, Guid>>();

            await bookRepository.InsertAsync(new Book()
            {
                Name = "Alpha Book", Type = BookType.Adventure,
                PublishDate = new DateTime(2001, 1, 1), Price = 1
            });
            await bookRepository.InsertAsync(new Book()
            {
                Name = "Beta Book", Type = BookType.Biography,
                PublishDate = new DateTime(2002, 1, 1), Price = 2
            });
            await bookRepository.InsertAsync(new Book()
            {
                Name = "Alpha Storm", Type = BookType.Dystopia,
                PublishDate = new DateTime(2003, 1, 1), Price = 3
            });

            await uow.CompleteAsync();
        }

        using (var scope = CreateScope())
        {
            using var uow = BeginUnitOfWork(scope);
            var bookRepository = scope.ServiceProvider.GetRequiredService<IBookRepository>();

            Assert.Equal(2, await bookRepository.GetCountAsync("Alpha"));

            var page1 = await bookRepository.GetPagedListAsync(0, 1, nameof(Book.Name), "Alpha");
            Assert.Single(page1);
            Assert.Equal("Alpha Book", page1[0].Name);

            var page2 = await bookRepository.GetPagedListAsync(1, 1, nameof(Book.Name), "Alpha");
            Assert.Equal("Alpha Storm", page2[0].Name);

            await uow.CompleteAsync();
        }
    }

    [Fact]
    public async Task Soft_delete_hides_books_and_ignore_filters_reveals()
    {
        if (!HasDatabase)
        {
            return;
        }

        var bookId = Guid.NewGuid();

        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);

        // Insert and COMMIT first — a GetAsync in the same unit of work, before
        // the insert is saved, would query the DB for a row that does not exist
        // yet and throw EntityNotFoundException.
        using (var scope = CreateScope())
        {
            using var uow = BeginUnitOfWork(scope);
            await scope.ServiceProvider
                .GetRequiredService<IRepository<Book, Guid>>()
                .InsertAsync(new Book(bookId)
                {
                    Name = "Fahrenheit 451",
                    Type = BookType.ScienceFiction,
                    PublishDate = new DateTime(1953, 10, 19),
                    Price = 12.5f
                });
            await uow.CompleteAsync();
        }

        // Soft-delete in a fresh unit of work.
        using (var scope = CreateScope())
        {
            using var uow = BeginUnitOfWork(scope);
            var book = await scope.ServiceProvider
                .GetRequiredService<IRepository<Book, Guid>>()
                .GetAsync(bookId);
            await scope.ServiceProvider
                .GetRequiredService<IRepository<Book, Guid>>()
                .DeleteAsync(book);
            await uow.CompleteAsync();
        }

        using (var scope = CreateScope())
        {
            using var uow = BeginUnitOfWork(scope);
            var bookRepository = scope.ServiceProvider.GetRequiredService<IRepository<Book, Guid>>();
            Assert.Equal(0, await bookRepository.GetCountAsync());

            var context = await scope.ServiceProvider
                .GetRequiredService<IDbContextProvider<BookStoreDbContext>>()
                .GetDbContextAsync();
            Assert.Equal(1, await context.Books.IgnoreQueryFilters().CountAsync());

            await uow.CompleteAsync();
        }
    }

    [Fact]
    public async Task BookManager_rejects_duplicate_name()
    {
        if (!HasDatabase)
        {
            return;
        }

        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);

        using (var scope = CreateScope())
        {
            using var uow = BeginUnitOfWork(scope);
            var bookManager = scope.ServiceProvider.GetRequiredService<BookManager>();
            var book = await bookManager.CreateAsync(
                BookType.Dystopia, "1984", new DateTime(1949, 6, 8), 19.84f);
            await scope.ServiceProvider
                .GetRequiredService<IRepository<Book, Guid>>()
                .InsertAsync(book);
            await uow.CompleteAsync();
        }

        using (var scope = CreateScope())
        {
            using var uow = BeginUnitOfWork(scope);
            var bookManager = scope.ServiceProvider.GetRequiredService<BookManager>();
            await Assert.ThrowsAsync<BusinessException>(() => bookManager.CreateAsync(
                BookType.ScienceFiction, "1984", new DateTime(2000, 1, 1), 1));
            await uow.CompleteAsync();
        }
    }
}
