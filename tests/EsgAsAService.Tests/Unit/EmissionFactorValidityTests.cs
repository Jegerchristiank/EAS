using EsgAsAService.Infrastructure.Data;
using EsgAsAService.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Tests.Unit;

public class EmissionFactorValidityTests
{
    private static EsgDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<EsgDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EsgDbContext(options);
    }

    [Fact]
    public async Task PicksMostRecentByValidFrom()
    {
        using var db = NewDb();
        var unit = new EsgAsAService.Domain.Entities.Core.Unit { Code = "kWh" }; db.Add(unit);
        db.Add(new EmissionFactorV2 { Country = "DK", Year = 2024, Type = "electricity", UnitId = unit.Id, Value = 0.1, ValidFrom = new DateOnly(2024,1,1) });
        db.Add(new EmissionFactorV2 { Country = "DK", Year = 2024, Type = "electricity", UnitId = unit.Id, Value = 0.09, ValidFrom = new DateOnly(2024,7,1) });
        await db.SaveChangesAsync();
        var ef = await db.EmissionFactorsV2.Where(f => f.Country=="DK" && f.Year==2024 && f.Type=="electricity").OrderByDescending(f=>f.ValidFrom).FirstAsync();
        Assert.Equal(0.09, ef.Value);
    }
}
