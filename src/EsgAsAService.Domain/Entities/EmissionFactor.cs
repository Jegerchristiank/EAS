using System.ComponentModel.DataAnnotations;

namespace EsgAsAService.Domain.Entities;

/// <summary>
/// An emission factor mapping to a category and unit.
/// Value is kg CO2e per unit.
/// </summary>
public class EmissionFactor
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Source { get; set; } = string.Empty; // e.g., Klimakompasset, EFRAG, DEFRA

    [Required, MaxLength(200)]
    public string Category { get; set; } = string.Empty; // e.g., Fuel/Diesel

    [Required, MaxLength(50)]
    public string Unit { get; set; } = string.Empty; // e.g., L, kWh, km

    [Range(0, double.MaxValue)]
    public double KgCO2ePerUnit { get; set; }

    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

