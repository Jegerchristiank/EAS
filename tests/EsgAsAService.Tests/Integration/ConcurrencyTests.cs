using System;
using System.Threading.Tasks;
using EsgAsAService.Domain.Entities;
using EsgAsAService.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EsgAsAService.Tests.Integration;

public class ConcurrencyTests
{
    private static EsgDbContext CreateDb(string dbName)
    {
        var opts = new DbContextOptionsBuilder<EsgDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new EsgDbContext(opts);
    }

    [Fact]
    public async Task ConcurrentUpserts_DoNotThrow_AfterRetry()
    {
    var dbName = "concurrency-shared-db-" + Guid.NewGuid();
    await using var db1 = CreateDb(dbName);
    var svc1 = new EsgDataService(db1);

        // Seed one reporting period
        var comp = new Company { Name = "Acme" };
        await svc1.UpsertCompanyAsync(comp);

        var p = new ReportingPeriod
        {
            CompanyId = comp.Id,
            Year = 2031,
            StartDate = new DateOnly(2031, 1, 1),
            EndDate = new DateOnly(2031, 12, 31)
        };
        await svc1.UpsertReportingPeriodAsync(p);

    // Create a second context/service to simulate concurrent editor (same in-memory DB)
    await using var db2 = CreateDb(dbName);
    var svc2 = new EsgDataService(db2);

        // Load the same period into two independent contexts
        var loaded1 = await svc1.GetReportingPeriodAsync(p.Id);
        var loaded2 = await svc2.GetReportingPeriodAsync(p.Id);

        // Make different updates
        loaded1!.Year = 2040;
        loaded2!.Year = 2050;

        // Save concurrently
        var t1 = svc1.UpsertReportingPeriodAsync(loaded1);
        var t2 = svc2.UpsertReportingPeriodAsync(loaded2);

        await Task.WhenAll(t1, t2);

        // One of the saves should have won; ensure DB has one entry with either year
        var final = await svc1.GetReportingPeriodAsync(p.Id);
        final.Should().NotBeNull();
        (final!.Year == 2040 || final.Year == 2050).Should().BeTrue();
    }
}
