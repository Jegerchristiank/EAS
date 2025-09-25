using System.ComponentModel.DataAnnotations;

namespace EsgAsAService.Domain.Entities;

/// <summary>
/// Represents a legal entity using the platform.
/// </summary>
public class Company
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Industry { get; set; }

    [MaxLength(2)]
    public string? CountryCode { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? OrganizationNumber { get; set; }

    public ICollection<ReportingPeriod> ReportingPeriods { get; set; } = new List<ReportingPeriod>();
}

