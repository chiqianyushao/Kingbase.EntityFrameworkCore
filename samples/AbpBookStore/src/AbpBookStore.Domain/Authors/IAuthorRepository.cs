using Volo.Abp.Domain.Repositories;

namespace AbpBookStore.Authors;

public interface IAuthorRepository : IRepository<Author, Guid>
{
    Task<Author?> FindByNameAsync(string name);
}
