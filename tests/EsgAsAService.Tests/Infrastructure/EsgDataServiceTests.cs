using System;
using System.Linq;
using System.Threading.Tasks;
using EsgAsAService.Domain.Entities;
using EsgAsAService.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EsgAsAService.Tests.Infrastructure;

public class EsgDataServiceTests
{
    private static EsgDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<EsgDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EsgDbContext(opts);
    }

    [Fact]
    public async Task UpsertCompanyAddsAndUpdatesWithAuditLogs()
    {
        await using var db = CreateDb();
        var svc = new EsgDataService(db);

        var c = new Company { Name = "Acme" };
        await svc.UpsertCompanyAsync(c);

        db.Companies.Count().Should().Be(1);
        db.AuditLogs.Count().Should().Be(1);

        c.Industry = "Manufacturing";
        await svc.UpsertCompanyAsync(c);

        db.Companies.Single().Industry.Should().Be("Manufacturing");
        db.AuditLogs.Count().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task UpsertReportingPeriodAddsAndGetsById()
    {
        await using var db = CreateDb();
        var svc = new EsgDataService(db);

        var company = new Company { Name = "Acme" };
        await svc.UpsertCompanyAsync(company);

        var p = new ReportingPeriod
        {
            CompanyId = company.Id,
            Year = 2030,
            StartDate = new DateOnly(2030, 1, 1),
            EndDate = new DateOnly(2030, 12, 31)
        };
        await svc.UpsertReportingPeriodAsync(p);

        var loaded = await svc.GetReportingPeriodAsync(p.Id);
        loaded.Should().NotBeNull();
        loaded!.CompanyId.Should().Be(company.Id);
        loaded.Year.Should().Be(2030);
    }
}
