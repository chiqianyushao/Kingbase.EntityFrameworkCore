using AbpBookStore.Books;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AbpBookStore.Books;

/// <summary>
/// Application service that reproduces the ABP BookStore list pattern
/// (paged + sorted + filtered through the custom repository) plus CRUD.
/// </summary>
public class BookAppService : ApplicationService, IBookAppService
{
    private readonly IBookRepository _bookRepository;

    public BookAppService(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<PagedResultDto<BookDto>> GetListAsync(GetBookListDto input)
    {
        var books = await _bookRepository.GetPagedListAsync(
            input.SkipCount, input.MaxResultCount, input.Sorting, input.Filter);
        var totalCount = await _bookRepository.GetCountAsync(input.Filter);

        return new PagedResultDto<BookDto>(
            totalCount,
            ObjectMapper.Map<List<Book>, List<BookDto>>(books));
    }

    public async Task<BookDto> GetAsync(Guid id)
        => ObjectMapper.Map<Book, BookDto>(await _bookRepository.GetAsync(id));

    public async Task<BookDto> CreateAsync(CreateUpdateBookDto input)
    {
        var book = new Book();
        ObjectMapper.Map(input, book);

        foreach (var authorId in input.AuthorIds)
        {
            book.BookAuthors.Add(new BookAuthor { AuthorId = authorId });
        }

        await _bookRepository.InsertAsync(book);
        return ObjectMapper.Map<Book, BookDto>(book);
    }

    public async Task<BookDto> UpdateAsync(Guid id, CreateUpdateBookDto input)
    {
        var book = await _bookRepository.GetAsync(id);
        ObjectMapper.Map(input, book);

        book.BookAuthors.Clear();
        foreach (var authorId in input.AuthorIds)
        {
            book.BookAuthors.Add(new BookAuthor { AuthorId = authorId });
        }

        await _bookRepository.UpdateAsync(book);
        return ObjectMapper.Map<Book, BookDto>(book);
    }

    public async Task DeleteAsync(Guid id)
        => await _bookRepository.DeleteAsync(id);
}
