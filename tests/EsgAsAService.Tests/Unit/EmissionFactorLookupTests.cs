using EsgAsAService.Infrastructure.Data;
using EsgAsAService.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Tests.Unit;

public class EmissionFactorLookupTests
{
    private static EsgDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<EsgDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EsgDbContext(options);
    }

    [Fact]
    public async Task LookupByCountryYearType()
    {
        using var db = NewDb();
        var unit = new EsgAsAService.Domain.Entities.Core.Unit { Code = "kWh" };
        db.Add(unit);
        db.Add(new EmissionFactorV2 { Country = "DK", Year = 2024, Type = "electricity", UnitId = unit.Id, Value = 0.1 });
        db.Add(new EmissionFactorV2 { Country = "DK", Year = 2023, Type = "electricity", UnitId = unit.Id, Value = 0.2 });
        await db.SaveChangesAsync();

        var results = await db.EmissionFactorsV2.Where(f => f.Country == "DK" && f.Year == 2024 && f.Type == "electricity").ToListAsync();
        Assert.Single(results);
        Assert.Equal(0.1, results[0].Value);
    }
}
