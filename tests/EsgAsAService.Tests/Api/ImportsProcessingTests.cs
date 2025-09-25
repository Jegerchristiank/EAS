using System.Text.Json;
using EsgAsAService.Api.Controllers.Importing;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Tests.Api;

public class ImportsProcessingTests
{
    private static EsgDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<EsgDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EsgDbContext(options);
    }

    [Fact]
    public async Task Water_Process_ImportsLines_AndIsIdempotent()
    {
        using var db = NewDb();
        var org = new Organisation { Name = "Org" }; db.Add(org);
        var rp = new ReportingPeriodV2 { OrganisationId = org.Id, Year = 2025, StartDate = new DateOnly(2025,1,1), EndDate = new DateOnly(2025,12,31) }; db.Add(rp);
        await db.SaveChangesAsync();

        var tmp = Path.GetTempFileName();
        await File.WriteAllTextAsync(tmp, "intake_m3,discharge_m3\n10,2\n5,\n");
        var meta = new { path = tmp, file = "water.csv", contentType = "text/csv", size = new FileInfo(tmp).Length };
        var doc = new StagingDocument { OrganisationId = org.Id, ReportingPeriodId = rp.Id, Type = "water_invoice", Source = "upload", PayloadJson = JsonSerializer.Serialize(meta) };
        db.Add(doc);
        await db.SaveChangesAsync();

        var ctrl = new WaterImportsController(db, new EsgAsAService.Api.Services.UnitConversionService(db));
        var res1 = await ctrl.Process(doc.Id) as OkObjectResult;
        Assert.NotNull(res1);
        Assert.Equal(2, db.WaterMeters.Count());

        // Idempotency: second run should not add more lines
        var res2 = await ctrl.Process(doc.Id) as OkObjectResult;
        Assert.NotNull(res2);
        Assert.Equal(2, db.WaterMeters.Count());

        try { File.Delete(tmp); } catch { }
    }

    [Fact]
    public async Task Water_Process_ConvertsUnits_WhenProvided()
    {
        using var db = NewDb();
        var org = new Organisation { Name = "Org" }; db.Add(org);
        var rp = new ReportingPeriodV2 { OrganisationId = org.Id, Year = 2025, StartDate = new DateOnly(2025,1,1), EndDate = new DateOnly(2025,12,31) }; db.Add(rp);
        // units and conversion L -> m3
        var uL = new EsgAsAService.Domain.Entities.Core.Unit { Code = "L" }; var uM3 = new EsgAsAService.Domain.Entities.Core.Unit { Code = "m3" };
        db.AddRange(uL, uM3);
        await db.SaveChangesAsync();
        db.Add(new UnitConversion { FromUnitId = uL.Id, ToUnitId = uM3.Id, Factor = 0.001 });
        await db.SaveChangesAsync();

        var tmp = Path.GetTempFileName();
        await File.WriteAllTextAsync(tmp, "intake,intake_unit,discharge,discharge_unit\n1000,L,500,L\n");
        var meta = new { path = tmp, file = "water.csv", contentType = "text/csv", size = new FileInfo(tmp).Length };
        var doc = new StagingDocument { OrganisationId = org.Id, ReportingPeriodId = rp.Id, Type = "water_invoice", Source = "upload", PayloadJson = JsonSerializer.Serialize(meta) };
        db.Add(doc);
        await db.SaveChangesAsync();

        var ctrl = new WaterImportsController(db, new EsgAsAService.Api.Services.UnitConversionService(db));
        var res = await ctrl.Process(doc.Id) as OkObjectResult;
        Assert.NotNull(res);
        var m = db.WaterMeters.Single();
        Assert.Equal(1.0, m.IntakeM3, 3);
        Assert.Equal(0.5, m.DischargeM3 ?? 0, 3);
        try { File.Delete(tmp); } catch { }
    }

    [Fact]
    public async Task Waste_Process_ImportsLines_AndIsIdempotent()
    {
        using var db = NewDb();
        var org = new Organisation { Name = "Org" }; db.Add(org);
        var rp = new ReportingPeriodV2 { OrganisationId = org.Id, Year = 2025, StartDate = new DateOnly(2025,1,1), EndDate = new DateOnly(2025,12,31) }; db.Add(rp);
        await db.SaveChangesAsync();

        var tmp = Path.GetTempFileName();
        await File.WriteAllTextAsync(tmp, "eak_code,quantity_kg,disposition\n200301,12.5,recycle\n150106,3.2,disposal\n");
        var meta = new { path = tmp, file = "waste.csv", contentType = "text/csv", size = new FileInfo(tmp).Length };
        var doc = new StagingDocument { OrganisationId = org.Id, ReportingPeriodId = rp.Id, Type = "waste_manifest", Source = "upload", PayloadJson = JsonSerializer.Serialize(meta) };
        db.Add(doc);
        await db.SaveChangesAsync();

        var ctrl = new WasteImportsController(db, new EsgAsAService.Api.Services.UnitConversionService(db));
        var res1 = await ctrl.Process(doc.Id) as OkObjectResult;
        Assert.NotNull(res1);
        Assert.Equal(2, db.WasteManifests.Count());

        // Idempotency: second run should not add more lines
        var res2 = await ctrl.Process(doc.Id) as OkObjectResult;
        Assert.NotNull(res2);
        Assert.Equal(2, db.WasteManifests.Count());

        try { File.Delete(tmp); } catch { }
    }

    [Fact]
    public async Task Waste_Process_ConvertsUnits_WhenProvided()
    {
        using var db = NewDb();
        var org = new Organisation { Name = "Org" }; db.Add(org);
        var rp = new ReportingPeriodV2 { OrganisationId = org.Id, Year = 2025, StartDate = new DateOnly(2025,1,1), EndDate = new DateOnly(2025,12,31) }; db.Add(rp);
        // units and conversion t -> kg
        var ut = new EsgAsAService.Domain.Entities.Core.Unit { Code = "t" }; var ukg = new EsgAsAService.Domain.Entities.Core.Unit { Code = "kg" };
        db.AddRange(ut, ukg);
        await db.SaveChangesAsync();
        db.Add(new UnitConversion { FromUnitId = ut.Id, ToUnitId = ukg.Id, Factor = 1000 });
        await db.SaveChangesAsync();

        var tmp = Path.GetTempFileName();
        await File.WriteAllTextAsync(tmp, "eak_code,quantity,unit,disposition\n200301,2,t,recycle\n");
        var meta = new { path = tmp, file = "waste.csv", contentType = "text/csv", size = new FileInfo(tmp).Length };
        var doc = new StagingDocument { OrganisationId = org.Id, ReportingPeriodId = rp.Id, Type = "waste_manifest", Source = "upload", PayloadJson = JsonSerializer.Serialize(meta) };
        db.Add(doc);
        await db.SaveChangesAsync();

        var ctrl = new WasteImportsController(db, new EsgAsAService.Api.Services.UnitConversionService(db));
        var res = await ctrl.Process(doc.Id) as OkObjectResult;
        Assert.NotNull(res);
        var w = db.WasteManifests.Single();
        Assert.Equal(2000, w.QuantityKg, 6);
        try { File.Delete(tmp); } catch { }
    }
}
