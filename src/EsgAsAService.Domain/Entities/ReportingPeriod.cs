using System.ComponentModel.DataAnnotations;

namespace EsgAsAService.Domain.Entities;

/// <summary>
/// Represents a reporting period (e.g., fiscal year) for ESG metrics.
/// </summary>
public class ReportingPeriod
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public int Year { get; set; }
    public bool IsCalendarYear { get; set; } = true;

    public ICollection<EnvironmentalActivity> EnvironmentalActivities { get; set; } = new List<EnvironmentalActivity>();
    public SocialIndicators? SocialIndicators { get; set; }
    public GovernancePractices? GovernancePractices { get; set; }
}

