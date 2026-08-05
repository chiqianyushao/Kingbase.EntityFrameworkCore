using Volo.Abp.Application.Dtos;

namespace AbpBookStore.Authors;

public class GetAuthorListDto : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
}
