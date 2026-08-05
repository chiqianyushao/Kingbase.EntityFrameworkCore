using System.ComponentModel.DataAnnotations;

namespace AbpBookStore.Books;

public class CreateUpdateBookDto
{
    [Required]
    [StringLength(BookConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    public BookType Type { get; set; }

    public DateTime PublishDate { get; set; }

    public float Price { get; set; }

    public List<Guid> AuthorIds { get; set; } = new();
}
