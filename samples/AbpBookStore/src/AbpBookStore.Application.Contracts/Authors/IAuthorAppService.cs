using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AbpBookStore.Authors;

public interface IAuthorAppService :
    IApplicationService
{
    Task<PagedResultDto<AuthorDto>> GetListAsync(GetAuthorListDto input);

    Task<AuthorDto> GetAsync(Guid id);

    Task<AuthorDto> CreateAsync(CreateUpdateAuthorDto input);

    Task<AuthorDto> UpdateAsync(Guid id, CreateUpdateAuthorDto input);

    Task DeleteAsync(Guid id);
}
