using AbpBookStore.Books;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AbpBookStore.EntityFrameworkCore.Books;

/// <summary>
/// EF Core repository reproducing the ABP BookStore tutorial's list query:
/// Include(BookAuthors.Author) + optional name filter + sort + Skip/Take, and a
/// matching count query. Uses plain LINQ so no ABP query-extension dependency.
/// </summary>
public class EfCoreBookRepository :
    EfCoreRepository<BookStoreDbContext, Book, Guid>,
    IBookRepository
{
    public EfCoreBookRepository(IDbContextProvider<BookStoreDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<List<Book>> GetPagedListAsync(
        int skipCount,
        int maxResultCount,
        string sorting,
        string? filter = null)
    {
        var queryable = await GetQueryableAsync();
        queryable = queryable
            .Include(b => b.BookAuthors)
            .ThenInclude(ba => ba.Author);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            queryable = queryable.Where(b => b.Name.Contains(filter));
        }

        queryable = sorting switch
        {
            nameof(Book.PublishDate) => queryable.OrderBy(b => b.PublishDate),
            nameof(Book.Price) => queryable.OrderBy(b => b.Price),
            _ => queryable.OrderBy(b => b.Name)
        };

        return await queryable
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToListAsync();
    }

    public async Task<long> GetCountAsync(string? filter = null)
    {
        var queryable = await GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            queryable = queryable.Where(b => b.Name.Contains(filter));
        }

        return await queryable.LongCountAsync();
    }

    public async Task<Book?> FindByNameAsync(string name)
        => await (await GetQueryableAsync()).FirstOrDefaultAsync(b => b.Name == name);
}
