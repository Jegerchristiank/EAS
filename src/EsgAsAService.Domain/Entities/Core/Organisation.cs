using System.ComponentModel.DataAnnotations;

namespace EsgAsAService.Domain.Entities.Core;

public class Organisation : RevisionedEntity
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Industry { get; set; }

    [MaxLength(2)]
    public string? CountryCode { get; set; }

    [MaxLength(100)]
    public string? OrganizationNumber { get; set; }
}

