using Volo.Abp.Domain.Entities.Auditing;

namespace AbpBookStore.Books;

/// <summary>
/// Mirrors the Book aggregate root from the ABP BookStore tutorial.
/// FullAuditedAggregateRoot adds audit fields, soft delete, a string
/// ConcurrencyStamp and the ExtraProperties JSON dictionary through ABP
/// conventions (ConfigureByConvention in the DbContext).
/// </summary>
public class Book : FullAuditedAggregateRoot<Guid>
{
    public Book()
    {
    }

    public Book(Guid id)
        : base(id)
    {
    }

    public string Name { get; set; } = string.Empty;

    public BookType Type { get; set; }

    public DateTime PublishDate { get; set; }

    public float Price { get; set; }

    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
}
