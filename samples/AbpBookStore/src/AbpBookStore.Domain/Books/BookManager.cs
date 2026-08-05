using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace AbpBookStore.Books;

/// <summary>
/// Domain service that guards book creation — mirrors the BookManager
/// from the ABP BookStore tutorial. Exercises a FindByNameAsync lookup.
/// </summary>
public class BookManager : DomainService
{
    private readonly IBookRepository _bookRepository;

    public BookManager(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<Book> CreateAsync(BookType type, string name, DateTime publishDate, float price)
    {
        var existingBook = await _bookRepository.FindByNameAsync(name);
        if (existingBook != null)
        {
            throw new BusinessException("BS:00001", "A book already exists with this name.");
        }

        return new Book
        {
            Type = type,
            Name = name,
            PublishDate = publishDate,
            Price = price
        };
    }

    public async Task ChangeNameAsync(Book book, string newName)
    {
        var existingBook = await _bookRepository.FindByNameAsync(newName);
        if (existingBook != null && existingBook.Id != book.Id)
        {
            throw new BusinessException("BS:00001", "A book already exists with this name.");
        }

        book.Name = newName;
    }
}
