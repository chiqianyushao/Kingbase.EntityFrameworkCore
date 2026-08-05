using Volo.Abp.Application.Dtos;

namespace AbpBookStore.Books;

public class GetBookListDto : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }

    public BookType? Type { get; set; }
}
