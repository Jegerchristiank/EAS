using System.Text;
using System.Threading.Tasks;
using EsgAsAService.Application.Abstractions;
using EsgAsAService.Domain.Entities;
using EsgAsAService.Infrastructure.Reporting;
using FluentAssertions;
using Moq;
using Xunit;

namespace EsgAsAService.Tests.Infrastructure;

public class ReportGeneratorTests
{
    [Fact]
    public async Task GeneratePdfReturnsPdfBytes()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var calc = new Mock<ICalculationService>();
        calc.Setup(c => c.SummarizeAsync(It.IsAny<ReportingPeriod>(), default))
            .ReturnsAsync(new EmissionSummary { Scope1Kg = 10, Scope2Kg = 20, Scope3Kg = 30 });
        var ai = new Mock<IAITextGenerator>();
        ai.Setup(x => x.GenerateAsync(It.IsAny<NarrativeRequest>(), default)).ReturnsAsync("narrative");
        var gen = new ReportGenerator(calc.Object, ai.Object);

        var company = new Company { Name = "Acme", CountryCode = "DK", Industry = "Manufacturing" };
        var period = new ReportingPeriod
        {
            Year = 2030,
            StartDate = new DateOnly(2030, 1, 1),
            EndDate = new DateOnly(2030, 12, 31)
        };

        var bytes = await gen.GeneratePdfAsync(company, period);
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(1000);
        // %PDF header
        Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public async Task GenerateXbrlContainsBasicTags()
    {
        var calc = new Mock<ICalculationService>();
        var ai = new Mock<IAITextGenerator>();
        ai.Setup(x => x.GenerateAsync(It.IsAny<NarrativeRequest>(), default)).ReturnsAsync("narrative");
        var gen = new ReportGenerator(calc.Object, ai.Object);

        var company = new Company { Name = "Acme", OrganizationNumber = "123" };
        var period = new ReportingPeriod
        {
            Year = 2030,
            StartDate = new DateOnly(2030, 1, 1),
            EndDate = new DateOnly(2030, 12, 31)
        };
        period.EnvironmentalActivities.Add(new Scope1Activity { CO2eKg = 10 });
        period.EnvironmentalActivities.Add(new Scope2Activity { CO2eKg = 5 });

        var bytes = await gen.GenerateXbrlAsync(company, period);
        var xml = Encoding.UTF8.GetString(bytes);
        xml.Should().Contain("xbrli:xbrl");
        xml.Should().Contain("Scope1Emissions");
        xml.Should().Contain("Scope2Emissions");
    }
}
