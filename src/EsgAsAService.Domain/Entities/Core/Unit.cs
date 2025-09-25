using System.ComponentModel.DataAnnotations;

namespace EsgAsAService.Domain.Entities.Core;

public class Unit : RevisionedEntity
{
    [Required, MaxLength(32)]
    public string Code { get; set; } = string.Empty; // e.g., L, kWh, kg, t

    [MaxLength(128)]
    public string? Name { get; set; }
}

public class UnitConversion : RevisionedEntity
{
    [Required]
    public Guid FromUnitId { get; set; }
    [Required]
    public Guid ToUnitId { get; set; }

    /// <summary>
    /// Multiply a value in FromUnit by Factor to get ToUnit.
    /// </summary>
    [Range(double.Epsilon, double.MaxValue)]
    public double Factor { get; set; }
}

