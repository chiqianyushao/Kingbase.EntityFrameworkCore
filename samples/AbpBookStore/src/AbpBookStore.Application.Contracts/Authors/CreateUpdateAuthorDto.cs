using System.ComponentModel.DataAnnotations;

namespace AbpBookStore.Authors;

public class CreateUpdateAuthorDto
{
    [Required]
    [StringLength(AuthorConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    public DateTime BirthDate { get; set; }

    public string? ShortBio { get; set; }
}
