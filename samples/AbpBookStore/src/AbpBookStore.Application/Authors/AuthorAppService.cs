using AbpBookStore.Authors;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AbpBookStore.Authors;

public class AuthorAppService : ApplicationService, IAuthorAppService
{
    private readonly IAuthorRepository _authorRepository;

    public AuthorAppService(IAuthorRepository authorRepository)
    {
        _authorRepository = authorRepository;
    }

    public async Task<PagedResultDto<AuthorDto>> GetListAsync(GetAuthorListDto input)
    {
        var queryable = await _authorRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            queryable = queryable.Where(a => a.Name.Contains(input.Filter));
        }

        var totalCount = await AsyncExecuter.CountAsync(queryable);
        var authors = await AsyncExecuter.ToListAsync(
            queryable.OrderBy(a => a.Name)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount));

        return new PagedResultDto<AuthorDto>(
            totalCount,
            ObjectMapper.Map<List<Author>, List<AuthorDto>>(authors));
    }

    public async Task<AuthorDto> GetAsync(Guid id)
        => ObjectMapper.Map<Author, AuthorDto>(await _authorRepository.GetAsync(id));

    public async Task<AuthorDto> CreateAsync(CreateUpdateAuthorDto input)
    {
        var author = new Author();
        ObjectMapper.Map(input, author);
        await _authorRepository.InsertAsync(author);
        return ObjectMapper.Map<Author, AuthorDto>(author);
    }

    public async Task<AuthorDto> UpdateAsync(Guid id, CreateUpdateAuthorDto input)
    {
        var author = await _authorRepository.GetAsync(id);
        ObjectMapper.Map(input, author);
        await _authorRepository.UpdateAsync(author);
        return ObjectMapper.Map<Author, AuthorDto>(author);
    }

    public async Task DeleteAsync(Guid id)
        => await _authorRepository.DeleteAsync(id);
}
