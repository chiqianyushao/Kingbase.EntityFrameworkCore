using AbpBookStore.Authors;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AbpBookStore.EntityFrameworkCore.Authors;

public class EfCoreAuthorRepository :
    EfCoreRepository<BookStoreDbContext, Author, Guid>,
    IAuthorRepository
{
    public EfCoreAuthorRepository(IDbContextProvider<BookStoreDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<Author?> FindByNameAsync(string name)
        => await (await GetQueryableAsync()).FirstOrDefaultAsync(a => a.Name == name);
}
