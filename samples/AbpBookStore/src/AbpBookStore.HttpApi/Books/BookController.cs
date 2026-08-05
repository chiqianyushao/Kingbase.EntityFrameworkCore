using AbpBookStore.Books;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace AbpBookStore.HttpApi.Books;

[RemoteService(Name = "BookStore")]
[Route("api/bookstore/books")]
public class BookController : AbpController, IBookAppService
{
    private readonly IBookAppService _bookAppService;

    public BookController(IBookAppService bookAppService)
    {
        _bookAppService = bookAppService;
    }

    [HttpGet]
    public Task<PagedResultDto<BookDto>> GetListAsync(GetBookListDto input)
        => _bookAppService.GetListAsync(input);

    [HttpGet("{id}")]
    public Task<BookDto> GetAsync(Guid id)
        => _bookAppService.GetAsync(id);

    [HttpPost]
    public Task<BookDto> CreateAsync(CreateUpdateBookDto input)
        => _bookAppService.CreateAsync(input);

    [HttpPut("{id}")]
    public Task<BookDto> UpdateAsync(Guid id, CreateUpdateBookDto input)
        => _bookAppService.UpdateAsync(id, input);

    [HttpDelete("{id}")]
    public Task DeleteAsync(Guid id)
        => _bookAppService.DeleteAsync(id);
}
