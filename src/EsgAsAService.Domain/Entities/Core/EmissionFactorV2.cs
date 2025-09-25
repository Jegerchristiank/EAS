using System.ComponentModel.DataAnnotations;

namespace EsgAsAService.Domain.Entities.Core;

public class EmissionFactorV2 : RevisionedEntity
{
    [Required, MaxLength(2)]
    public string Country { get; set; } = ""; // ISO 3166-1 alpha-2

    [Range(1900, 3000)]
    public int Year { get; set; }

    [Required, MaxLength(100)]
    public string Type { get; set; } = string.Empty; // electricity, diesel, etc

    [Required]
    public Guid UnitId { get; set; } // unit the factor applies to

    /// <summary>
    /// kg CO2e per Unit
    /// </summary>
    [Range(0, double.MaxValue)]
    public double Value { get; set; }

    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }

    public Guid? DataSourceId { get; set; }
}

