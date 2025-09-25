using System.ComponentModel.DataAnnotations;

namespace EsgAsAService.Domain.Entities.Core;

public class SectionMetricInput : RevisionedEntity
{
    [Required] public Guid OrganisationId { get; set; }
    [Required] public Guid ReportingPeriodId { get; set; }

    [Required, MaxLength(10)] public string Section { get; set; } = string.Empty; // e.g. B3, C1
    [Required, MaxLength(50)] public string Metric { get; set; } = string.Empty;  // e.g. total_kg

    public double? NumericValue { get; set; }
    [MaxLength(2000)] public string? TextValue { get; set; }
    [MaxLength(16)] public string? Unit { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
}
