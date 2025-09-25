using System.ComponentModel.DataAnnotations;

namespace EsgAsAService.Domain.Entities.Core;

public class ReportingPeriodV2 : RevisionedEntity
{
    [Required]
    public Guid OrganisationId { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    [Range(1900, 3000)]
    public int Year { get; set; }
}

