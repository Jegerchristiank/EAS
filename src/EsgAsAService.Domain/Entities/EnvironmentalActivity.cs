using System.ComponentModel.DataAnnotations;

namespace EsgAsAService.Domain.Entities;

public enum EmissionScope
{
    Scope1 = 1,
    Scope2 = 2,
    Scope3 = 3
}

public enum Scope2Method
{
    Unknown = 0,
    LocationBased = 1,
    MarketBased = 2
}

/// <summary>
/// Base class for environmental activities contributing to GHG emissions.
/// </summary>
public abstract class EnvironmentalActivity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReportingPeriodId { get; set; }
    public ReportingPeriod? ReportingPeriod { get; set; }

    public abstract EmissionScope Scope { get; }

    /// <summary>
    /// Quantity of activity measured (e.g., liters, kWh, km, kg, EUR, etc.).
    /// </summary>
    [Range(0, double.MaxValue)]
    public double Quantity { get; set; }

    /// <summary>
    /// Unit of measure for the quantity (e.g., L, kWh, km, kg, tkm, EUR).
    /// </summary>
    [MaxLength(50)]
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Category path for mapping against emission factors (e.g., "Fuel/Diesel")
    /// </summary>
    [MaxLength(200)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Emission factor value used (kg CO2e per unit)
    /// </summary>
    [Range(0, double.MaxValue)]
    public double EmissionFactorKgPerUnit { get; set; }

    /// <summary>
    /// Resulting CO2e in kilograms for this activity.
    /// </summary>
    [Range(0, double.MaxValue)]
    public double CO2eKg { get; set; }
}

public class Scope1Activity : EnvironmentalActivity
{
    public override EmissionScope Scope => EmissionScope.Scope1;

    /// <summary>
    /// Optional detail like fuel type (Diesel, Gasoline, NaturalGas).
    /// </summary>
    [MaxLength(100)]
    public string? TypeDetail { get; set; }
}

public class Scope2Activity : EnvironmentalActivity
{
    public override EmissionScope Scope => EmissionScope.Scope2;
    public Scope2Method Method { get; set; } = Scope2Method.LocationBased;
}

public class Scope3Activity : EnvironmentalActivity
{
    public override EmissionScope Scope => EmissionScope.Scope3;

    /// <summary>
    /// GHG Protocol Scope 3 Category (1..15).
    /// </summary>
    [Range(1, 15)]
    public int CategoryNumber { get; set; }

    [MaxLength(300)]
    public string? Description { get; set; }
}

