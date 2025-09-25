using EsgAsAService.Application.Abstractions;
using EsgAsAService.Application.Services;
using EsgAsAService.Domain.Entities;
using FluentAssertions;

namespace EsgAsAService.Tests;

public class CalculationServiceTests
{
    private class FakeProvider : IEmissionFactorProvider
    {
        public Task<EmissionFactor?> GetFactorAsync(string category, string unit, DateOnly? onDate = null, CancellationToken ct = default)
        {
            var v = (category, unit) switch
            {
                ("Fuel/Diesel", "L") => 2.68,
                ("Electricity/Consumption", "kWh") => 0.30,
                _ => 0
            };
            if (v <= 0) return Task.FromResult<EmissionFactor?>(null);
            return Task.FromResult<EmissionFactor?>(new EmissionFactor { Category = category, Unit = unit, KgCO2ePerUnit = v, Source = "Test" });
        }
    }

    [Fact]
    public async Task CalculatesScope1Diesel()
    {
        var calc = new CalculationService(new FakeProvider());
        var act = new Scope1Activity { Category = "Fuel/Diesel", Unit = "L", Quantity = 100 };
        await calc.CalculateAsync(act);
        act.CO2eKg.Should().BeApproximately(268, 0.01);
    }

    [Fact]
    public async Task SummarizeAggregatesScopes()
    {
        var calc = new CalculationService(new FakeProvider());
        var period = new ReportingPeriod
        {
            EnvironmentalActivities =
            {
                new Scope1Activity { Category = "Fuel/Diesel", Unit = "L", Quantity = 10 },
                new Scope2Activity { Category = "Electricity/Consumption", Unit = "kWh", Quantity = 100 }
            }
        };

        foreach (var a in period.EnvironmentalActivities)
            await calc.CalculateAsync(a);

        var s = await calc.SummarizeAsync(period);
        s.Scope1Kg.Should().BeApproximately(26.8, 0.01);
        s.Scope2Kg.Should().BeApproximately(30, 0.01);
        s.TotalKg.Should().BeApproximately(56.8, 0.01);
    }
}

