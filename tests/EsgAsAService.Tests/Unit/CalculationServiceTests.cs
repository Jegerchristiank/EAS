using EsgAsAService.Api.Services;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Tests.Unit;

public class CalculationServiceTests
{
    private static EsgDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<EsgDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EsgDbContext(options);
    }

    [Fact]
    public async Task Computes_Co2e_Deterministically()
    {
        using var db = NewDb();
        var unit = new EsgAsAService.Domain.Entities.Core.Unit { Code = "kWh" };
        db.Add(unit);
        var factor = new EmissionFactorV2 { Country = "DK", Year = 2024, Type = "electricity", UnitId = unit.Id, Value = 0.2 };
        db.Add(factor);
        var org = new Organisation { Name = "Org" }; db.Add(org);
        var rp = new ReportingPeriodV2 { OrganisationId = org.Id, Year = 2024, StartDate = new DateOnly(2024,1,1), EndDate = new DateOnly(2024,12,31) }; db.Add(rp);
        var act = new Activity { OrganisationId = org.Id, ReportingPeriodId = rp.Id, Category = "electricity", Quantity = 1000, UnitId = unit.Id };
        db.Add(act);
        var se = new ScopeEntry { ActivityId = act.Id, Scope = 2, EmissionFactorId = factor.Id, Adjustment = 1.0 };
        db.Add(se);
        // Add a deviation to satisfy precondition
        db.Add(new Deviation { ScopeEntryId = se.Id, Reason = "evidence waived for test" });
        await db.SaveChangesAsync();

        var unitsSvc = new UnitConversionService(db);
        var calc = new CalculationRunner(db, unitsSvc);
        var n = await calc.RunAsync(rp.Id);
        Assert.Equal(1, n);
        var res = db.CalculationResults.Single();
        Assert.Equal(1000, res.QuantityNormalized, 5);
        Assert.Equal(0.2, res.Factor, 5);
        Assert.Equal(200.0, res.Co2eKg, 5);
    }
}
