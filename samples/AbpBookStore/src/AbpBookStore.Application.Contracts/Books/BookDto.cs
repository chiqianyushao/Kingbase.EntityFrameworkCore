using AbpBookStore.Authors;
using Volo.Abp.Application.Dtos;

namespace AbpBookStore.Books;

public class BookDto : AuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;

    public BookType Type { get; set; }

    public DateTime PublishDate { get; set; }

    public float Price { get; set; }

    public List<AuthorDto> Authors { get; set; } = new();
}
