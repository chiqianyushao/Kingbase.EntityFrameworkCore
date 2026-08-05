using AbpBookStore.Authors;

namespace AbpBookStore.Books;

/// <summary>
/// Explicit many-to-many join entity between Book and Author, with a
/// composite primary key (BookId, AuthorId) — mirrors the ABP BookStore
/// tutorial. Both foreign keys are configured with DeleteBehavior.Cascade.
/// </summary>
public class BookAuthor
{
    public Guid BookId { get; set; }

    public Book Book { get; set; } = default!;

    public Guid AuthorId { get; set; }

    public Author Author { get; set; } = default!;
}
