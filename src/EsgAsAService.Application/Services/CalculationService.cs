using EsgAsAService.Application.Abstractions;
using EsgAsAService.Domain.Entities;

namespace EsgAsAService.Application.Services;

/// <summary>
/// Default calculation logic: CO2e (kg) = Quantity * EmissionFactorKgPerUnit.
/// If emission factor is missing (0), attempts to fetch from providers.
/// </summary>
public class CalculationService(IEmissionFactorProvider emissionFactorProvider) : ICalculationService
{
    private readonly IEmissionFactorProvider _factors = emissionFactorProvider;

    public async Task CalculateAsync(EnvironmentalActivity activity, CancellationToken ct = default)
    {
        if (activity.EmissionFactorKgPerUnit <= 0)
        {
            var factor = await _factors.GetFactorAsync(activity.Category, activity.Unit, null, ct);
            if (factor is not null)
            {
                activity.EmissionFactorKgPerUnit = factor.KgCO2ePerUnit;
            }
        }

        activity.CO2eKg = Math.Max(0, activity.Quantity) * Math.Max(0, activity.EmissionFactorKgPerUnit);
    }

    public Task<EmissionSummary> SummarizeAsync(ReportingPeriod period, CancellationToken ct = default)
    {
        var summary = new EmissionSummary();
        foreach (var a in period.EnvironmentalActivities)
        {
            switch (a.Scope)
            {
                case EmissionScope.Scope1:
                    summary.Scope1Kg += a.CO2eKg;
                    break;
                case EmissionScope.Scope2:
                    summary.Scope2Kg += a.CO2eKg;
                    break;
                case EmissionScope.Scope3:
                    summary.Scope3Kg += a.CO2eKg;
                    break;
            }
        }
        return Task.FromResult(summary);
    }
}

