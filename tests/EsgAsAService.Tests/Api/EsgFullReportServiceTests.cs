using System.Text.Json;
using EsgAsAService.Api.Services;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EsgAsAService.Tests.Api;

public class EsgFullReportServiceTests
{
    private static EsgDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<EsgDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EsgDbContext(options);
    }

    [Fact]
    public async Task Generate_ReturnsBasicSections()
    {
        using var db = NewDb();
        var org = new Organisation { Name = "Org", OrganizationNumber = "CVR-123" }; db.Add(org);
        var period = new ReportingPeriodV2 { OrganisationId = org.Id, Year = 2025, StartDate = new DateOnly(2025,1,1), EndDate = new DateOnly(2025,12,31) }; db.Add(period);
        var unit = new EsgAsAService.Domain.Entities.Core.Unit { Code = "kWh" }; db.Add(unit);
        await db.SaveChangesAsync();

        var act = new Activity { OrganisationId = org.Id, ReportingPeriodId = period.Id, Category = "electricity", Quantity = 0, UnitId = unit.Id }; db.Add(act);
        var se = new ScopeEntry { ActivityId = act.Id, Scope = 2 }; db.Add(se);
        var cr = new CalculationResult { ScopeEntryId = se.Id, QuantityNormalized = 1000, Factor = 0.2, Co2eKg = 200 }; db.Add(cr);
        db.Add(new Financials { OrganisationId = org.Id, ReportingPeriodId = period.Id, Revenue = 1000, Currency = "DKK" });
        db.Add(new WaterMeter { OrganisationId = org.Id, ReportingPeriodId = period.Id, IntakeM3 = 10, DischargeM3 = 2 });
        await db.SaveChangesAsync();

        var svc = new EsgFullReportService(db);
        var payload = await svc.GenerateAsync(period.Id);
        var json = JsonSerializer.Serialize(payload);
        Assert.Contains("\"B3\":", json);
        Assert.Contains("\"scope2_kg\":200", json);
        Assert.Contains("\"consumption_m3\":8", json);
    }
}
