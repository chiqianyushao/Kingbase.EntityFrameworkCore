using AbpBookStore.Authors;
using AbpBookStore.Books;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace AbpBookStore.EntityFrameworkCore;

/// <summary>
/// Mirrors the BookStoreDataSeeder from the ABP BookStore tutorial:
/// seeds two authors and three books linked through BookAuthor.
/// </summary>
public class BookStoreDataSeeder : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<Book, Guid> _bookRepository;
    private readonly IRepository<Author, Guid> _authorRepository;
    private readonly IGuidGenerator _guidGenerator;

    public BookStoreDataSeeder(
        IRepository<Book, Guid> bookRepository,
        IRepository<Author, Guid> authorRepository,
        IGuidGenerator guidGenerator)
    {
        _bookRepository = bookRepository;
        _authorRepository = authorRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        if (await _bookRepository.GetCountAsync() > 0)
        {
            return;
        }

        var orwell = new Author(_guidGenerator.Create())
        {
            Name = "George Orwell",
            BirthDate = new DateTime(1903, 6, 25),
            ShortBio = "English novelist, essayist, journalist and critic."
        };

        var adams = new Author(_guidGenerator.Create())
        {
            Name = "Douglas Adams",
            BirthDate = new DateTime(1952, 3, 11),
            ShortBio = "English author, humorist and dramatist."
        };

        await _authorRepository.InsertManyAsync(new[] { orwell, adams });

        await _bookRepository.InsertManyAsync(new[]
        {
            new Book(_guidGenerator.Create())
            {
                Name = "1984",
                Type = BookType.Dystopia,
                PublishDate = new DateTime(1949, 6, 8),
                Price = 19.84f,
                BookAuthors = new List<BookAuthor>
                {
                    new BookAuthor { AuthorId = orwell.Id }
                }
            },
            new Book(_guidGenerator.Create())
            {
                Name = "The Hitchhiker's Guide to the Galaxy",
                Type = BookType.ScienceFiction,
                PublishDate = new DateTime(1979, 10, 12),
                Price = 42.0f,
                BookAuthors = new List<BookAuthor>
                {
                    new BookAuthor { AuthorId = adams.Id }
                }
            },
            new Book(_guidGenerator.Create())
            {
                Name = "Animal Farm",
                Type = BookType.Poetry,
                PublishDate = new DateTime(1945, 8, 17),
                Price = 9.99f,
                BookAuthors = new List<BookAuthor>
                {
                    new BookAuthor { AuthorId = orwell.Id }
                }
            }
        });
    }
}
