using System;
using System.Text;
using System.Linq;
using EsgAsAService.Api.Controllers;
using EsgAsAService.Api.Models;
using EsgAsAService.Api.Models.Vsme;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using EsgAsAService.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EsgAsAService.Tests.Api;

public class ControllersBasicTests
{
    private static EsgDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<EsgDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EsgDbContext(options);
    }

    [Fact]
    public async Task NarrativesUpsertAndGet()
    {
        using var db = NewDb();
        var org = new Organisation { Name = "Org" };
        db.Add(org);
        await db.SaveChangesAsync();

        var period = new ReportingPeriodV2
        {
            OrganisationId = org.Id,
            Year = 2024,
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 12, 31)
        };
        db.Add(period);
        await db.SaveChangesAsync();

        var ctrl = new NarrativesController(db);
        var req = new NarrativeUpsertRequest(
            new C1StrategySection { Summary = "Net zero roadmap", TargetYear = 2030 },
            new C2RiskSection { Process = "TCFD-aligned process", ClimateRisks = "Transition risk", Opportunities = "Energy efficiency" },
            new C3HumanRightsSection { PolicyExists = true, DueDiligenceInPlace = true, HighRiskAreas = "Supply chain" },
            new C4GovernanceSection { BoardOversight = "Quarterly review", ClimateExpertOnBoard = true },
            new C5BoardDiversitySection { PercentFemale = 0.45, PercentIndependent = 0.6, DiversityPolicy = "50/50 by 2026" },
            new C6StakeholderSection { StakeholderGroups = "Employees, suppliers", EngagementProcess = "Workshops" },
            new C7ValueChainSection { UpstreamCoverage = "Tier 1 suppliers", DownstreamCoverage = "Major distributors" },
            new C8AssuranceSection { Provider = "DNV", AssuranceLevel = "Limited" },
            new C9MethodologySection { ReportingBoundary = "Operational control", EmissionFactorSources = "IEA 2024" }
        );

        var put = await ctrl.Upsert(period.Id, req, CancellationToken.None);
        Assert.IsType<NoContentResult>(put);

        var get = await ctrl.Get(period.Id, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(get.Result);
        var payload = Assert.IsType<NarrativeResponse>(ok.Value);
        Assert.Equal(period.Id, payload.PeriodId);
        Assert.Equal("Net zero roadmap", payload.C1!.Summary);
        Assert.Equal("TCFD-aligned process", payload.C2!.Process);
        Assert.True(payload.C3!.PolicyExists);
        Assert.Equal("Quarterly review", payload.C4!.BoardOversight);
        Assert.Equal(0.45, payload.C5!.PercentFemale);
        Assert.Equal("Employees, suppliers", payload.C6!.StakeholderGroups);
        Assert.Equal("Tier 1 suppliers", payload.C7!.UpstreamCoverage);
        Assert.Equal("DNV", payload.C8!.Provider);
        Assert.Equal("Operational control", payload.C9!.ReportingBoundary);
    }

    [Fact]
    public async Task SectionMetricsUpsertAndGet()
    {
        using var db = NewDb();
        var org = new Organisation { Name = "MetricOrg" };
        db.Add(org);
        await db.SaveChangesAsync();

        var period = new ReportingPeriodV2
        {
            OrganisationId = org.Id,
            Year = 2025,
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 12, 31)
        };
        db.Add(period);
        await db.SaveChangesAsync();

        var ctrl = new SectionMetricsController(db);
        var request = new SectionMetricsUpsertRequest(new List<SectionMetricUpsertEntry>
        {
            new("B3", "total_kg", 1234.5, null, "kg", "manual override"),
            new("C2", "risk_score", null, "High", null, "qualitative input")
        });

        var put = await ctrl.Upsert(period.Id, request, CancellationToken.None);
        Assert.IsType<NoContentResult>(put);

        var get = await ctrl.Get(period.Id, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(get.Result);
        var payload = Assert.IsType<SectionMetricsResponse>(ok.Value);
        Assert.Equal(2, payload.Metrics.Count);
        var b3 = payload.Metrics.First(m => m.Metric == "total_kg");
        Assert.True(b3.Value.HasValue);
        Assert.Equal(1234.5, b3.Value.Value, 1);
        Assert.Equal("kg", b3.Unit);
        var c2 = payload.Metrics.First(m => m.Metric == "risk_score");
        Assert.Equal("High", c2.Text);
    }

    [Fact]
    public async Task ActivitiesCreateNullRequestReturnsBadRequest()
    {
        using var db = NewDb();
        var ctrl = new ActivitiesController(db);
        var res = await ctrl.Create(null!);
        Assert.IsType<BadRequestResult>(res.Result);
    }

    [Fact]
    public async Task ActivitiesCreatePersistsAndReturnsCreated()
    {
        using var db = NewDb();
        var ctrl = new ActivitiesController(db);
        var req = new ActivityRequest(Guid.NewGuid(), Guid.NewGuid(), "Fuel/Diesel", null, 10, Guid.NewGuid());
        var result = await ctrl.Create(req);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(ActivitiesController.Get), created.ActionName);
        var payload = Assert.IsType<IdResponse>(created.Value);
        Assert.NotEqual(Guid.Empty, payload.Id);
        Assert.Single(db.Activities);
    }

    [Fact]
    public async Task ReportingPeriodsCreateAndUpdateRespectNulls()
    {
        using var db = NewDb();
        var ctrl = new ReportingPeriodsController(db);
        var bad = await ctrl.Create(null!);
        Assert.IsType<BadRequestResult>(bad.Result);

        var org = new Organisation { Name = "Org" }; db.Add(org); await db.SaveChangesAsync();
        var req = new ReportingPeriodRequest(org.Id, 2024, new DateOnly(2024,1,1), new DateOnly(2024,12,31));
        var created = await ctrl.Create(req);
        var createdAt = Assert.IsType<CreatedAtActionResult>(created.Result);
        var id = ((IdResponse)createdAt.Value!).Id;

        var badUpdate = await ctrl.Update(id, null!);
        Assert.IsType<BadRequestResult>(badUpdate.Result);

        var ok = await ctrl.Update(id, req);
        Assert.IsType<OkObjectResult>(ok.Result);
    }

    [Fact]
    public async Task OrganisationsCreateAndUpdate()
    {
        using var db = NewDb();
        var ctrl = new OrganisationsController(db);
        var bad = await ctrl.Create(null!);
        Assert.IsType<BadRequestResult>(bad.Result);
        var req = new OrganisationRequest("Acme", "Manufacturing", "DK", "123");
        var created = await ctrl.Create(req);
        var createdAt = Assert.IsType<CreatedAtActionResult>(created.Result);
        var id = ((IdResponse)createdAt.Value!).Id;
        var badUpdate = await ctrl.Update(id, null!);
        Assert.IsType<BadRequestResult>(badUpdate.Result);
        var ok = await ctrl.Update(id, req);
        Assert.IsType<OkObjectResult>(ok.Result);
    }

    [Fact]
    public async Task EmissionFactorsCreateReturnsCreated()
    {
        using var db = NewDb();
        var ctrl = new EmissionFactorsController(db);
        var bad = await ctrl.Create(null!);
        Assert.IsType<BadRequestResult>(bad.Result);
        var unit = new EsgAsAService.Domain.Entities.Core.Unit { Code = "kWh" }; db.Add(unit); await db.SaveChangesAsync();
        var req = new EsgAsAService.Api.Models.EmissionFactorRequest(
            Country: "DK",
            Year: 2024,
            Type: "Electricity",
            UnitId: unit.Id,
            Value: 0.2,
            ValidFrom: null,
            ValidTo: null,
            DataSourceId: null
        );
        var created = await ctrl.Create(req);
        var createdRes = Assert.IsType<CreatedAtActionResult>(created.Result);
        Assert.Equal(nameof(EmissionFactorsController.Get), createdRes.ActionName);
        var payload = Assert.IsType<IdResponse>(createdRes.Value);
        Assert.NotEqual(Guid.Empty, payload.Id);
    }

    [Fact]
    public async Task DataSourcesCreateReturnsCreated()
    {
        using var db = NewDb();
        var ctrl = new DataSourcesController(db);
        var bad = await ctrl.Create(null!);
        Assert.IsType<BadRequestResult>(bad.Result);
        var req = new DataSource { Name = "Source", Url = "https://example.com" };
        var created = await ctrl.Create(req);
        // Controller returns CreatedAtActionResult; assert result type and that id is present in the body
        var createdAt = Assert.IsType<CreatedAtActionResult>(created.Result);
        var id = ((IdResponse)createdAt.Value!).Id;
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task DeviationsCreateReturnsCreated()
    {
        using var db = NewDb();
        var ctrl = new DeviationsController(db);
        var bad = await ctrl.Create(null!);
        Assert.IsType<BadRequestResult>(bad.Result);
        var req = new DeviationRequest(Guid.NewGuid(), "Manual correction");
        var created = await ctrl.Create(req);
        var createdRes = Assert.IsType<CreatedAtActionResult>(created.Result);
        Assert.Equal(nameof(DeviationsController.Get), createdRes.ActionName);
        var payload = Assert.IsType<IdResponse>(createdRes.Value);
        Assert.NotEqual(Guid.Empty, payload.Id);
    }

    [Fact]
    public async Task ApprovalsSubmitAndPatch()
    {
        using var db = NewDb();
        var ctrl = new ApprovalsController(db);
        var bad = await ctrl.Submit(null!);
        Assert.IsType<BadRequestResult>(bad);
        var ap = await ctrl.Submit(new ApprovalSubmitRequest(Guid.NewGuid()));
        var ok = Assert.IsType<OkObjectResult>(ap);
        var id = ((IdResponse)ok.Value!).Id;
        // Patch without comment should fail for Approved/Rejected
        var badPatch = await ctrl.Patch(id, new ApprovalPatchRequest("Approved", null));
        Assert.IsType<BadRequestObjectResult>(badPatch);
        var goodPatch = await ctrl.Patch(id, new ApprovalPatchRequest("Approved", "LGTM"));
        Assert.IsType<OkObjectResult>(goodPatch);
    }

    [Fact]
    public void EvidencePresignReturnsAbsoluteUri()
    {
        using var db = NewDb();
        var ctrl = new EvidenceController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        ctrl.HttpContext.Request.Scheme = "http";
        ctrl.HttpContext.Request.Host = new HostString("localhost", 5198);
        var res = ctrl.GetPresigned(new EvidenceUploadRequest(Guid.NewGuid(), "file.pdf"));
        var ok = Assert.IsType<OkObjectResult>(res.Result);
        var payload = Assert.IsType<EvidenceUploadResponse>(ok.Value);
        Assert.True(payload.UploadUrl.IsAbsoluteUri);
    }
}
