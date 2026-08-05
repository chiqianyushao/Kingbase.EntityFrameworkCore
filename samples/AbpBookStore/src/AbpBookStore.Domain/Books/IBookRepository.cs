using Volo.Abp.Domain.Repositories;

namespace AbpBookStore.Books;

/// <summary>
/// Repository contract matching the ABP BookStore tutorial: the default
/// GetPagedListAsync implementation lives in the EF Core layer.
/// </summary>
public interface IBookRepository : IRepository<Book, Guid>
{
    Task<List<Book>> GetPagedListAsync(
        int skipCount,
        int maxResultCount,
        string sorting,
        string? filter = null);

    Task<long> GetCountAsync(string? filter = null);

    Task<Book?> FindByNameAsync(string name);
}
