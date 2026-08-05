using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AbpBookStore.Books;

public interface IBookAppService :
    IApplicationService
{
    Task<PagedResultDto<BookDto>> GetListAsync(GetBookListDto input);

    Task<BookDto> GetAsync(Guid id);

    Task<BookDto> CreateAsync(CreateUpdateBookDto input);

    Task<BookDto> UpdateAsync(Guid id, CreateUpdateBookDto input);

    Task DeleteAsync(Guid id);
}
