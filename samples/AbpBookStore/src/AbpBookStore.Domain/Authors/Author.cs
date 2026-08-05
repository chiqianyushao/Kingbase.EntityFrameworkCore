using Volo.Abp.Domain.Entities.Auditing;

namespace AbpBookStore.Authors;

/// <summary>
/// Mirrors the Author aggregate root from the ABP BookStore tutorial.
/// </summary>
public class Author : FullAuditedAggregateRoot<Guid>
{
    public Author()
    {
    }

    public Author(Guid id)
        : base(id)
    {
    }

    public string Name { get; set; } = string.Empty;

    public DateTime BirthDate { get; set; }

    public string? ShortBio { get; set; }

    public ICollection<Books.BookAuthor> Books { get; set; } = new List<Books.BookAuthor>();
}
