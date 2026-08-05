using Volo.Abp.Application.Dtos;

namespace AbpBookStore.Authors;

public class AuthorDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;

    public DateTime BirthDate { get; set; }

    public string? ShortBio { get; set; }
}
