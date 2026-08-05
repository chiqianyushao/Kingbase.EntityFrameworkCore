using AbpBookStore.Authors;
using AbpBookStore.Books;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Uow;

namespace AbpBookStore.EntityFrameworkCore.Tests.Capability;

/// <summary>
/// Base for the report-driven capability tests. These exercise the capabilities
/// listed in docs/EFCore10-KingbaseES-Compatibility-Report.md THROUGH the real
/// ABP application stack (module-booted DbContext, IRepository, unit of work)
/// against a real KingbaseES database. The report's full matrix is declared in
/// CapabilityItems.cs and rendered by CapabilityReportGeneratorTests.
///
/// Each test class derives from AbpBookStoreTestBase, so it is in the
/// "Abp book store" non-parallel collection and inherits the shared fixture.
/// Tests early-return (silently skip) when no database is configured, keeping
/// `dotnet test` green offline — same convention as the rest of the sample.
/// </summary>
public abstract class CapabilityTestBase : AbpBookStoreTestBase
{
    protected CapabilityTestBase(AbpAppFixture fixture, ITestOutputHelper output)
        : base(fixture, output)
    {
    }

    protected async Task<BookStoreDbContext> GetDbContextAsync(IServiceScope scope)
        => await scope.ServiceProvider
            .GetRequiredService<IDbContextProvider<BookStoreDbContext>>()
            .GetDbContextAsync();

    /// <summary>Opens a fresh ABP unit of work + scope, hands the DbContext to the
    /// action and completes the UoW. Queries are read-only; the UoW commit is a
    /// no-op for them but keeps the ABP session semantics identical to app code.</summary>
    protected async Task<T> InDbContextAsync<T>(Func<BookStoreDbContext, Task<T>> action)
    {
        using var scope = CreateScope();
        using var uow = BeginUnitOfWork(scope);
        var context = await GetDbContextAsync(scope);
        var result = await action(context);
        await uow.CompleteAsync();
        return result;
    }

    /// <summary>Drops and recreates the three BookStore tables so each capability
    /// test starts from a clean, deterministic dataset in the shared database.</summary>
    protected async Task ResetSchemaAsync()
        => await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);

    protected async Task SeedBooksAsync(params Book[] books)
    {
        using var scope = CreateScope();
        using var uow = BeginUnitOfWork(scope);
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<Book, Guid>>();
        await repository.InsertManyAsync(books);
        await uow.CompleteAsync();
    }

    protected async Task SeedAuthorsAsync(params Author[] authors)
    {
        using var scope = CreateScope();
        using var uow = BeginUnitOfWork(scope);
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<Author, Guid>>();
        await repository.InsertManyAsync(authors);
        await uow.CompleteAsync();
    }

    /// <summary>A Book with a stable id, so each test seeds deterministic rows.</summary>
    protected static Book NewBook(string name, BookType type, DateTime publishDate, float price, Guid? id = null)
        => new(id ?? Guid.NewGuid())
        {
            Name = name,
            Type = type,
            PublishDate = publishDate,
            Price = price
        };

    protected static Author NewAuthor(string name, DateTime birthDate, Guid? id = null)
        => new(id ?? Guid.NewGuid())
        {
            Name = name,
            BirthDate = birthDate
        };
}
