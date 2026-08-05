using AbpBookStore.Authors;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace AbpBookStore.HttpApi.Authors;

[RemoteService(Name = "BookStore")]
[Route("api/bookstore/authors")]
public class AuthorController : AbpController, IAuthorAppService
{
    private readonly IAuthorAppService _authorAppService;

    public AuthorController(IAuthorAppService authorAppService)
    {
        _authorAppService = authorAppService;
    }

    [HttpGet]
    public Task<PagedResultDto<AuthorDto>> GetListAsync(GetAuthorListDto input)
        => _authorAppService.GetListAsync(input);

    [HttpGet("{id}")]
    public Task<AuthorDto> GetAsync(Guid id)
        => _authorAppService.GetAsync(id);

    [HttpPost]
    public Task<AuthorDto> CreateAsync(CreateUpdateAuthorDto input)
        => _authorAppService.CreateAsync(input);

    [HttpPut("{id}")]
    public Task<AuthorDto> UpdateAsync(Guid id, CreateUpdateAuthorDto input)
        => _authorAppService.UpdateAsync(id, input);

    [HttpDelete("{id}")]
    public Task DeleteAsync(Guid id)
        => _authorAppService.DeleteAsync(id);
}
