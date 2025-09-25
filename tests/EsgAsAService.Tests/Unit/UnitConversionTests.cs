using EsgAsAService.Api.Services;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Tests.Unit;

public class UnitConversionTests
{
    private static EsgDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<EsgDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EsgDbContext(options);
    }

    [Fact]
    public async Task ConvertsByFactor()
    {
        using var db = NewDb();
        var from = new EsgAsAService.Domain.Entities.Core.Unit { Code = "kWh" };
        var to = new EsgAsAService.Domain.Entities.Core.Unit { Code = "MWh" };
        db.AddRange(from, to);
        db.Add(new UnitConversion { FromUnitId = from.Id, ToUnitId = to.Id, Factor = 0.001 });
        await db.SaveChangesAsync();

        var svc = new UnitConversionService(db);
        var outVal = await svc.ConvertAsync(from.Id, to.Id, 1234);
        Assert.Equal(1.234, outVal, 3);
    }

    [Fact]
    public async Task ThrowsWhenMissingConversion()
    {
        using var db = NewDb();
        var from = new EsgAsAService.Domain.Entities.Core.Unit { Code = "L" };
        var to = new EsgAsAService.Domain.Entities.Core.Unit { Code = "kg" };
        db.AddRange(from, to);
        await db.SaveChangesAsync();

        var svc = new UnitConversionService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ConvertAsync(from.Id, to.Id, 1));
    }
}
