using EsgAsAService.Domain.Entities;

namespace EsgAsAService.Application.Abstractions;

public class EmissionSummary
{
    public double Scope1Kg { get; set; }
    public double Scope2Kg { get; set; }
    public double Scope3Kg { get; set; }
    public double TotalKg => Scope1Kg + Scope2Kg + Scope3Kg;
}

/// <summary>
/// Calculates CO2e for activities and aggregates across a reporting period.
/// </summary>
public interface ICalculationService
{
    Task CalculateAsync(EnvironmentalActivity activity, CancellationToken ct = default);
    Task<EmissionSummary> SummarizeAsync(ReportingPeriod period, CancellationToken ct = default);
}

