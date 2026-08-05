using AbpBookStore.Authors;
using AbpBookStore.Books;
using Kdbndp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Uow;

namespace AbpBookStore.EntityFrameworkCore.Tests;

/// <summary>
/// Targets the ABP <-> provider combination points that the provider's own
/// integration suite does not cover: ExtraProperties JSON value converter,
/// string ConcurrencyStamp optimistic concurrency, the explicit BookAuthor
/// join entity (composite key + cascade) and the SaveChanges interceptor
/// pipeline.
/// </summary>
public sealed class ModelBehaviorTests : AbpBookStoreTestBase
{
    public ModelBehaviorTests(AbpAppFixture fixture, ITestOutputHelper output)
        : base(fixture, output)
    {
    }

    [Fact]
    public async Task ExtraProperties_jsonb_roundtrip()
    {
        if (!HasDatabase)
        {
            return;
        }

        var bookId = Guid.NewGuid();

        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);

        using (var scope = CreateScope())
        {
            using var uow = BeginUnitOfWork(scope);
            var book = new Book(bookId)
            {
                Name = "Extra Properties",
                Type = BookType.Horror,
                PublishDate = new DateTime(2020, 1, 1),
                Price = 5
            };
            book.ExtraProperties["note"] = "hello from ExtraProperties";
            book.ExtraProperties["rating"] = 5;

            await scope.ServiceProvider
                .GetRequiredService<IRepository<Book, Guid>>()
                .InsertAsync(book);
            await uow.CompleteAsync();
        }

        using (var scope = CreateScope())
        {
            using var uow = BeginUnitOfWork(scope);
            var loaded = await scope.ServiceProvider
                .GetRequiredService<IRepository<Book, Guid>>()
                .GetAsync(bookId);

            Assert.Equal("hello from ExtraProperties", loaded.ExtraProperties["note"]?.ToString());
            Assert.Equal("5", loaded.ExtraProperties["rating"]?.ToString());

            await uow.CompleteAsync();
        }
    }

    [Fact]
    public async Task ConcurrencyStamp_stale_update_throws()
    {
        if (!HasDatabase)
        {
            return;
        }

        var bookId = Guid.NewGuid();

        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);

        using (var scope = CreateScope())
        {
            using var uow = BeginUnitOfWork(scope);
            await scope.ServiceProvider
                .GetRequiredService<IRepository<Book, Guid>>()
                .InsertAsync(new Book(bookId)
                {
                    Name = "Concurrency",
                    Type = BookType.Science,
                    PublishDate = new DateTime(2010, 5, 5),
                    Price = 10
                });
            await uow.CompleteAsync();
        }

        // NOTE on the "second writer": ABP UoWs created while another UoW is
        // ambient become children and share the parent's DbContext, so two nested
        // scopes would load the SAME entity instance (no stale stamp at all).
        // Instead, simulate a concurrent writer by updating the ConcurrencyStamp
        // directly in the database after our entity is loaded.
        using (var scope = CreateScope())
        {
            using var uow = BeginUnitOfWork(scope);
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Book, Guid>>();
            var book = await repository.GetAsync(bookId);

            await using var connection = new KdbndpConnection(Fixture.ConnectionString!);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                "UPDATE \"Books\" SET \"ConcurrencyStamp\" = 'concurrent-writer' WHERE \"Id\" = @id";
            var idParameter = command.CreateParameter();
            idParameter.ParameterName = "id";
            idParameter.Value = bookId;
            command.Parameters.Add(idParameter);
            await command.ExecuteNonQueryAsync();

            // ABP wraps EF's DbUpdateConcurrencyException into its own type.
            book.Name = "changed by stale writer";
            await Assert.ThrowsAsync<AbpDbConcurrencyException>(() => uow.CompleteAsync());
        }
    }

    [Fact]
    public async Task BookAuthor_composite_key_include_and_cascade_delete()
    {
        if (!HasDatabase)
        {
            return;
        }

        var bookId = Guid.NewGuid();
        var author1Id = Guid.NewGuid();
        var author2Id = Guid.NewGuid();

        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);

        using (var scope = CreateScope())
        {
            using var uow = BeginUnitOfWork(scope);
            var authorRepository = scope.ServiceProvider.GetRequiredService<IRepository<Author, Guid>>();
            await authorRepository.InsertAsync(new Author(author1Id)
            {
                Name = "Author One", BirthDate = new DateTime(1960, 1, 1)
            });
            await authorRepository.InsertAsync(new Author(author2Id)
            {
                Name = "Author Two", BirthDate = new DateTime(1970, 1, 1)
            });

            var book = new Book(bookId)
            {
                Name = "Co-authored",
                Type = BookType.Science,
                PublishDate = new DateTime(2021, 1, 1),
                Price = 15
            };
            book.BookAuthors.Add(new BookAuthor { AuthorId = author1Id, Book = book });
            book.BookAuthors.Add(new BookAuthor { AuthorId = author2Id, Book = book });

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
            Assert.Equal(2, loaded.BookAuthors.Count);

            var names = loaded.BookAuthors.Select(ba => ba.Author.Name).OrderBy(n => n).ToList();
            Assert.Equal(["Author One", "Author Two"], names);

            await uow.CompleteAsync();
        }

        // Hard-delete the Book; its BookAuthor rows must cascade away.
        // NOTE: Book is a soft-delete aggregate, so context.Remove / repository
        // Delete only SOFT-delete it (no DB cascade, join rows stay). Use the
        // repository's HardDeleteAsync so the DB ON DELETE CASCADE fires.
        using (var scope = CreateScope())
        {
            using var uow = BeginUnitOfWork(scope);
            var bookRepository = scope.ServiceProvider.GetRequiredService<IRepository<Book, Guid>>();
            var book = await bookRepository.GetAsync(bookId);

            await bookRepository.HardDeleteAsync(book);
            await uow.CompleteAsync();
        }

        using (var scope = CreateScope())
        {
            using var uow = BeginUnitOfWork(scope);
            var context = await scope.ServiceProvider
                .GetRequiredService<IDbContextProvider<BookStoreDbContext>>()
                .GetDbContextAsync();

            // Raw counts: BookAuthors has no soft-delete filter, Books does.
            Assert.Equal(0, await context.BookAuthors.CountAsync());
            Assert.Equal(0, await context.Books.IgnoreQueryFilters().CountAsync());

            await uow.CompleteAsync();
        }
    }

    [Fact]
    public async Task SaveChanges_interceptor_fires()
    {
        if (!HasDatabase)
        {
            return;
        }

        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);

        using var scope = CreateScope();
        var interceptor = scope.ServiceProvider.GetRequiredService<TestSaveChangesInterceptor>();

        using var uow = BeginUnitOfWork(scope);
        await scope.ServiceProvider
            .GetRequiredService<IRepository<Book, Guid>>()
            .InsertAsync(new Book()
            {
                Name = "Interceptor Probe",
                Type = BookType.Biography,
                PublishDate = new DateTime(1990, 3, 3),
                Price = 3
            });
        await uow.CompleteAsync();

        Assert.True(interceptor.SavingCalls > 0, "The save-changes interceptor did not fire.");
    }
}
