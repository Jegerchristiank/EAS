using System.Text;
using EsgAsAService.Api.Services;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Tests.Unit;

public class CsvImportTests
{
    private static EsgDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<EsgDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EsgDbContext(options);
    }

    private static IFormFile MakeCsv(string content, string name = "file.csv")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var ms = new MemoryStream(bytes);
        return new FormFile(ms, 0, bytes.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv"
        };
    }

    [Fact]
    public async Task AnalyzeReturnsHeaders()
    {
        var csv = "date,category,quantity,unitCode\n2024-01-01,electricity,100,kWh\n";
        var file = MakeCsv(csv);
        using var db = NewDb();
        var svc = new CsvImportService(db, new UnitConversionService(db));
        var res = await svc.AnalyzeAsync(file, default);
        Assert.Contains("category", res.Headers);
        Assert.Equal(1, res.RowCount);
    }

    [Fact]
    public async Task CommitPersistsActivitiesAndScopeEntries()
    {
        using var db = NewDb();
        var org = new Organisation { Name = "Org" }; db.Add(org);
        var rp = new ReportingPeriodV2 { OrganisationId = org.Id, Year = 2024, StartDate = new DateOnly(2024,1,1), EndDate = new DateOnly(2024,12,31) }; db.Add(rp);
        var unit = new EsgAsAService.Domain.Entities.Core.Unit { Code = "kWh" }; db.Add(unit);
        await db.SaveChangesAsync();

        var csv = "date,category,quantity,unitCode,to\n2024-01-01,electricity,100,kWh,kWh\n";
        var file = MakeCsv(csv);
        var svc = new CsvImportService(db, new UnitConversionService(db));
        var mapping = new CsvCommitRequest(new Dictionary<string,string>{{"date","date"},{"category","category"},{"quantity","quantity"},{"unitCode","unitCode"},{"toUnitCode","to"}});
        var n = await svc.CommitAsync(file, mapping, org.Id, rp.Id, default);
        Assert.Equal(1, n);
        Assert.Single(db.Activities);
        Assert.Single(db.ScopeEntries);
    }
}
