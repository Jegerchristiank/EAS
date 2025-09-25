using System.Threading.Tasks;
using EsgAsAService.Application.Abstractions;
using EsgAsAService.Application.Services;
using EsgAsAService.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace EsgAsAService.Tests.Unit;

public class CalculationClampTests
{
    [Fact]
    public async Task NegativeQuantity_IsClampedToZero()
    {
        var factors = new Mock<IEmissionFactorProvider>();
        factors.Setup(x => x.GetFactorAsync(It.IsAny<string>(), It.IsAny<string>(), null, default))
            .ReturnsAsync((EsgAsAService.Domain.Entities.EmissionFactor?)null);
        var svc = new CalculationService(factors.Object);

        var a = new Scope1Activity { Quantity = -5, EmissionFactorKgPerUnit = 10, Unit = "L", Category = "Fuel/Diesel" };
        await svc.CalculateAsync(a);
        a.CO2eKg.Should().Be(0);
    }

    [Fact]
    public async Task Summarize_Aggregates_Scopes_Correctly()
    {
        var factors = new Mock<IEmissionFactorProvider>();
        var svc = new CalculationService(factors.Object);
        var p = new ReportingPeriod();
        p.EnvironmentalActivities.Add(new Scope1Activity { CO2eKg = 10 });
        p.EnvironmentalActivities.Add(new Scope2Activity { CO2eKg = 20 });
        p.EnvironmentalActivities.Add(new Scope3Activity { CO2eKg = 30 });

        var summary = await svc.SummarizeAsync(p);
        summary.Scope1Kg.Should().Be(10);
        summary.Scope2Kg.Should().Be(20);
        summary.Scope3Kg.Should().Be(30);
        summary.TotalKg.Should().Be(60);
    }
}
