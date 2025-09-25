using EsgAsAService.Application.Abstractions;
using EsgAsAService.Domain.Entities;

namespace EsgAsAService.Infrastructure.EmissionFactors;

/// <summary>
/// Static fallback emission factors for common categories.
/// Values are indicative only and should be replaced with authoritative sources.
/// </summary>
public class StaticEmissionFactorProvider : IEmissionFactorProvider
{
    private readonly Dictionary<(string category, string unit), double> _factors = new()
    {
        [("Fuel/Diesel", "L")] = 2.68, // kg CO2e per liter (approx)
        [("Electricity/Consumption", "kWh")] = 0.30, // location-based, example value
        [("Transport/PassengerCar", "km")] = 0.120, // per km, example
        [("Heat/DistrictHeating", "kWh")] = 0.15,
    };

    public Task<EmissionFactor?> GetFactorAsync(string category, string unit, DateOnly? onDate = null, CancellationToken ct = default)
    {
        if (_factors.TryGetValue((category, unit), out var v))
        {
            return Task.FromResult<EmissionFactor?>(new EmissionFactor
            {
                Source = "Static",
                Category = category,
                Unit = unit,
                KgCO2ePerUnit = v
            });
        }

        return Task.FromResult<EmissionFactor?>(null);
    }
}
