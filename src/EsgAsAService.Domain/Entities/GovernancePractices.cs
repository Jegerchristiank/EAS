using System.ComponentModel.DataAnnotations;

namespace EsgAsAService.Domain.Entities;

/// <summary>
/// Governance-related practices and indicators.
/// </summary>
public class GovernancePractices
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReportingPeriodId { get; set; }
    public ReportingPeriod? ReportingPeriod { get; set; }

    [Range(0, 100)]
    public double PercentFemaleBoard { get; set; }

    public bool AntiCorruptionPolicy { get; set; }
    public bool DataPrivacyPolicy { get; set; }
    public bool WhistleblowerPolicy { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

