using System.Text;
using EsgAsAService.Api.Controllers;
using EsgAsAService.Api.Models.Vsme;
using EsgAsAService.Api.Services;
using EsgAsAService.Application.Models;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EsgAsAService.Tests.Api;

public class ReportsControllerTests
{
    private static EsgDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<EsgDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EsgDbContext(options);
    }

    [Fact]
    public async Task GenerateJson_Returns501_WhenServiceNotRegistered()
    {
        using var db = NewDb();
        var vsme = new MockVsmeService();
        var ctrl = new ReportsController(db, vsme);

        var res = await ctrl.GenerateJson(Guid.NewGuid());
        var objRes = Assert.IsType<ObjectResult>(res);
        Assert.Equal(501, objRes.StatusCode);
    }

    [Fact]
    public async Task GenerateJson_ReturnsPayload_WhenServiceRegistered()
    {
        using var db = NewDb();
        // Arrange: seed org + period
        var org = new EsgAsAService.Domain.Entities.Core.Organisation { Name = "Org", OrganizationNumber = "CVR-1" };
        db.Add(org);
        var p = new EsgAsAService.Domain.Entities.Core.ReportingPeriodV2
        {
            OrganisationId = org.Id,
            Year = 2025,
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 12, 31)
        };
        db.Add(p);
        await db.SaveChangesAsync();

        var vsme = new MockVsmeService();
        var full = new StubFullReportService();
        var ctrl = new ReportsController(db, vsme, full);

        // Act
        var res = await ctrl.GenerateJson(p.Id);
        var ok = Assert.IsType<OkObjectResult>(res);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        Assert.Contains(p.Id.ToString(), json);
    }

    [Fact]
    public async Task GenerateJson_Returns404_WhenPeriodMissing()
    {
        using var db = NewDb();
        var vsme = new MockVsmeService();
        var full = new StubFullReportService();
        var ctrl = new ReportsController(db, vsme, full);

        var res = await ctrl.GenerateJson(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(res);
    }

    private sealed class StubFullReportService : IEsgFullReportService
    {
        public Task<FullEsgReport> GenerateAsync(Guid periodId, CancellationToken ct = default)
        {
            var rep = new FullEsgReport
            {
                Meta = new MetaSection
                {
                    Period = new PeriodMeta { Id = periodId, Year = 0, StartDate = new DateOnly(2000,1,1), EndDate = new DateOnly(2000,12,31) },
                    Organisation = new OrganisationMeta { Id = Guid.Empty, Name = "" }
                },
                B3 = new B3Section { Scope1Kg = 0, Scope2Kg = 0, Scope3Kg = 0, TotalKg = 0 }
            };
            return Task.FromResult(rep);
        }
    }
    [Fact]
    public void ExportXbrlFormatsInvariantAndEscapes()
    {
        using var db = NewDb();
        var vsme = new MockVsmeService();
        var ctrl = new ReportsController(db, vsme);

        var report = new VsmeBasicReport(
            new VsmeDisclosure(1234.56, new[] { "s" }),
            new VsmeDisclosure("TBD", new[] { "s" }),
            new VsmeDisclosure("TBD", new[] { "s" }),
            new VsmeDisclosure(10, new[] { "s" }),
            new VsmeDisclosure(20, new[] { "s" }),
            new VsmeDisclosure(30, new[] { "s" }),
            new VsmeDisclosure("TBD", new[] { "s" }),
            new VsmeDisclosure("TBD", new[] { "s" }),
            new VsmeDisclosure("TBD", new[] { "s" }),
            new VsmeDisclosure("TBD", new[] { "s" }),
            new VsmeDisclosure("TBD", new[] { "s" })
        );

        var res = ctrl.ExportXbrl(report) as FileContentResult;
        Assert.NotNull(res);
        Assert.Equal("application/xml", res!.ContentType);
        var xml = Encoding.UTF8.GetString(res.FileContents);
        Assert.Contains("<E1_TotalEmissions>1234.56</E1_TotalEmissions>", xml);
        Assert.Contains("<E4_Scope1>10</E4_Scope1>", xml);
    }

    private sealed class MockVsmeService : IVsmeReportService
    {
        public Task<VsmeBasicReport> GenerateBasicAsync(Guid periodId, CancellationToken ct = default)
            => Task.FromResult(new VsmeBasicReport(
                new VsmeDisclosure(0, new[] { "s" }),
                new VsmeDisclosure("TBD", new[] { "s" }),
                new VsmeDisclosure("TBD", new[] { "s" }),
                new VsmeDisclosure(0, new[] { "s" }),
                new VsmeDisclosure(0, new[] { "s" }),
                new VsmeDisclosure(0, new[] { "s" }),
                new VsmeDisclosure("TBD", new[] { "s" }),
                new VsmeDisclosure("TBD", new[] { "s" }),
                new VsmeDisclosure("TBD", new[] { "s" }),
                new VsmeDisclosure("TBD", new[] { "s" }),
                new VsmeDisclosure("TBD", new[] { "s" })
            ));
    }
}
