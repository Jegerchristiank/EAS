using System.ComponentModel.DataAnnotations;

namespace EsgAsAService.Domain.Entities;

/// <summary>
/// Social indicators aligned with VSME social disclosures.
/// </summary>
public class SocialIndicators
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReportingPeriodId { get; set; }
    public ReportingPeriod? ReportingPeriod { get; set; }

    [Range(0, int.MaxValue)]
    public int EmployeeCount { get; set; }

    [Range(0, 100)]
    public double PercentFemaleEmployees { get; set; }

    [Range(0, double.MaxValue)]
    public double TotalHoursWorked { get; set; }

    [Range(0, double.MaxValue)]
    public double TrainingHours { get; set; }

    [Range(0, double.MaxValue)]
    public double LostTimeInjuries { get; set; }
}

