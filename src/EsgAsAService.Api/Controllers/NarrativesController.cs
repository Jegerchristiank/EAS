using EsgAsAService.Api.Models;
using EsgAsAService.Application.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("narratives")]
public class NarrativesController(EsgDbContext db) : ControllerBase
{
    private readonly EsgDbContext _db = db;

    [HttpGet("{periodId:guid}")]
    [Authorize(Policy = "CanRead")]
    public async Task<ActionResult<NarrativeResponse>> Get(Guid periodId, CancellationToken ct)
    {
        var period = await _db.ReportingPeriodsV2.AsNoTracking().FirstOrDefaultAsync(p => p.Id == periodId, ct);
        if (period is null) return NotFound();

        var response = new NarrativeResponse(
            periodId,
            await LoadC1Async(periodId, ct),
            await LoadC2Async(periodId, ct),
            await LoadC3Async(periodId, ct),
            await LoadC4Async(periodId, ct),
            await LoadC5Async(periodId, ct),
            await LoadC6Async(periodId, ct),
            await LoadC7Async(periodId, ct),
            await LoadC8Async(periodId, ct),
            await LoadC9Async(periodId, ct)
        );

        return Ok(response);
    }

    [HttpPut("{periodId:guid}")]
    [Authorize(Policy = "CanIngestData")]
    public async Task<IActionResult> Upsert(Guid periodId, [FromBody] NarrativeUpsertRequest request, CancellationToken ct)
    {
        if (request is null) return BadRequest();

        var period = await _db.ReportingPeriodsV2.FirstOrDefaultAsync(p => p.Id == periodId, ct);
        if (period is null) return NotFound();

        var orgId = period.OrganisationId;

        if (request.C1 is not null)
        {
            await UpsertStrategyAsync(orgId, periodId, request.C1, ct);
        }

        if (request.C2 is not null)
        {
            await UpsertRiskAsync(orgId, periodId, request.C2, ct);
        }

        if (request.C3 is not null)
        {
            await UpsertHumanRightsAsync(orgId, periodId, request.C3, ct);
        }

        if (request.C4 is not null)
        {
            await UpsertGovernanceAsync(orgId, periodId, request.C4, ct);
        }

        if (request.C5 is not null)
        {
            await UpsertBoardAsync(orgId, periodId, request.C5, ct);
        }

        if (request.C6 is not null)
        {
            await UpsertStakeholderAsync(orgId, periodId, request.C6, ct);
        }

        if (request.C7 is not null)
        {
            await UpsertValueChainAsync(orgId, periodId, request.C7, ct);
        }

        if (request.C8 is not null)
        {
            await UpsertAssuranceAsync(orgId, periodId, request.C8, ct);
        }

        if (request.C9 is not null)
        {
            await UpsertMethodologyAsync(orgId, periodId, request.C9, ct);
        }

        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    private async Task<C1StrategySection?> LoadC1Async(Guid periodId, CancellationToken ct)
    {
        var entity = await _db.StrategyTargets.AsNoTracking().FirstOrDefaultAsync(x => x.ReportingPeriodId == periodId, ct);
        if (entity is null) return null;
        return new C1StrategySection
        {
            Summary = entity.Summary,
            ShortTermTarget = entity.ShortTermTarget,
            LongTermTarget = entity.LongTermTarget,
            EmissionReductionTargetPct = entity.EmissionReductionTargetPct,
            TargetYear = entity.TargetYear,
            InvestmentPlan = entity.InvestmentPlan,
            Progress = entity.Progress
        };
    }

    private async Task<C2RiskSection?> LoadC2Async(Guid periodId, CancellationToken ct)
    {
        var entity = await _db.RiskAssessments.AsNoTracking().FirstOrDefaultAsync(x => x.ReportingPeriodId == periodId, ct);
        if (entity is null) return null;
        return new C2RiskSection
        {
            Process = entity.Process,
            ClimateRisks = entity.ClimateRisks,
            Opportunities = entity.Opportunities,
            TimeHorizon = entity.TimeHorizon,
            Mitigations = entity.Mitigations
        };
    }

    private async Task<C3HumanRightsSection?> LoadC3Async(Guid periodId, CancellationToken ct)
    {
        var entity = await _db.HumanRightsAssessments.AsNoTracking().FirstOrDefaultAsync(x => x.ReportingPeriodId == periodId, ct);
        if (entity is null) return null;
        return new C3HumanRightsSection
        {
            PolicyExists = entity.PolicyExists,
            DueDiligenceInPlace = entity.DueDiligenceInPlace,
            HighRiskAreas = entity.HighRiskAreas,
            Remediation = entity.Remediation,
            TrainingProvided = entity.TrainingProvided
        };
    }

    private async Task<C4GovernanceSection?> LoadC4Async(Guid periodId, CancellationToken ct)
    {
        var entity = await _db.GovernanceOversights.AsNoTracking().FirstOrDefaultAsync(x => x.ReportingPeriodId == periodId, ct);
        if (entity is null) return null;
        return new C4GovernanceSection
        {
            BoardOversight = entity.BoardOversight,
            ManagementResponsibilities = entity.ManagementResponsibilities,
            Incentives = entity.Incentives,
            ClimateExpertOnBoard = entity.ClimateExpertOnBoard
        };
    }

    private async Task<C5BoardDiversitySection?> LoadC5Async(Guid periodId, CancellationToken ct)
    {
        var entity = await _db.BoardDiversities.AsNoTracking().FirstOrDefaultAsync(x => x.ReportingPeriodId == periodId, ct);
        if (entity is null) return null;
        return new C5BoardDiversitySection
        {
            PercentFemale = entity.PercentFemale,
            PercentMale = entity.PercentMale,
            PercentOther = entity.PercentOther,
            PercentIndependent = entity.PercentIndependent,
            DiversityPolicy = entity.DiversityPolicy,
            SelectionProcess = entity.SelectionProcess
        };
    }

    private async Task<C6StakeholderSection?> LoadC6Async(Guid periodId, CancellationToken ct)
    {
        var entity = await _db.StakeholderEngagements.AsNoTracking().FirstOrDefaultAsync(x => x.ReportingPeriodId == periodId, ct);
        if (entity is null) return null;
        return new C6StakeholderSection
        {
            StakeholderGroups = entity.StakeholderGroups,
            EngagementProcess = entity.EngagementProcess,
            KeyTopics = entity.KeyTopics,
            WorkerRepresentation = entity.WorkerRepresentation
        };
    }

    private async Task<C7ValueChainSection?> LoadC7Async(Guid periodId, CancellationToken ct)
    {
        var entity = await _db.ValueChainCoverages.AsNoTracking().FirstOrDefaultAsync(x => x.ReportingPeriodId == periodId, ct);
        if (entity is null) return null;
        return new C7ValueChainSection
        {
            UpstreamCoverage = entity.UpstreamCoverage,
            DownstreamCoverage = entity.DownstreamCoverage,
            Scope3Categories = entity.Scope3Categories,
            DataGaps = entity.DataGaps
        };
    }

    private async Task<C8AssuranceSection?> LoadC8Async(Guid periodId, CancellationToken ct)
    {
        var entity = await _db.AssuranceActivities.AsNoTracking().FirstOrDefaultAsync(x => x.ReportingPeriodId == periodId, ct);
        if (entity is null) return null;
        return new C8AssuranceSection
        {
            Provider = entity.Provider,
            AssuranceLevel = entity.AssuranceLevel,
            Scope = entity.Scope,
            AssuranceDate = entity.AssuranceDate,
            Summary = entity.Summary,
            IsIndependent = entity.IsIndependent
        };
    }

    private async Task<C9MethodologySection?> LoadC9Async(Guid periodId, CancellationToken ct)
    {
        var entity = await _db.MethodologyStatements.AsNoTracking().FirstOrDefaultAsync(x => x.ReportingPeriodId == periodId, ct);
        if (entity is null) return null;
        return new C9MethodologySection
        {
            ReportingBoundary = entity.ReportingBoundary,
            ConsolidationApproach = entity.ConsolidationApproach,
            EmissionFactorSources = entity.EmissionFactorSources,
            EstimationApproach = entity.EstimationApproach,
            MaterialityThreshold = entity.MaterialityThreshold
        };
    }

    private async Task UpsertStrategyAsync(Guid orgId, Guid periodId, C1StrategySection section, CancellationToken ct)
    {
        var entity = await _db.StrategyTargets.FirstOrDefaultAsync(x => x.ReportingPeriodId == periodId, ct);
        if (entity is null)
        {
            entity = new StrategyTarget { OrganisationId = orgId, ReportingPeriodId = periodId };
            await _db.StrategyTargets.AddAsync(entity, ct);
        }

        entity.Summary = section.Summary;
        entity.ShortTermTarget = section.ShortTermTarget;
        entity.LongTermTarget = section.LongTermTarget;
        entity.EmissionReductionTargetPct = section.EmissionReductionTargetPct;
        entity.TargetYear = section.TargetYear;
        entity.InvestmentPlan = section.InvestmentPlan;
        entity.Progress = section.Progress;
    }

    private async Task UpsertRiskAsync(Guid orgId, Guid periodId, C2RiskSection section, CancellationToken ct)
    {
        var entity = await _db.RiskAssessments.FirstOrDefaultAsync(x => x.ReportingPeriodId == periodId, ct);
        if (entity is null)
        {
            entity = new RiskAssessment { OrganisationId = orgId, ReportingPeriodId = periodId };
            await _db.RiskAssessments.AddAsync(entity, ct);
        }

        entity.Process = section.Process;
        entity.ClimateRisks = section.ClimateRisks;
        entity.Opportunities = section.Opportunities;
        entity.TimeHorizon = section.TimeHorizon;
        entity.Mitigations = section.Mitigations;
    }

    private async Task UpsertHumanRightsAsync(Guid orgId, Guid periodId, C3HumanRightsSection section, CancellationToken ct)
    {
        var entity = await _db.HumanRightsAssessments.FirstOrDefaultAsync(x => x.ReportingPeriodId == periodId, ct);
        if (entity is null)
        {
            entity = new HumanRightsAssessment { OrganisationId = orgId, ReportingPeriodId = periodId };
            await _db.HumanRightsAssessments.AddAsync(entity, ct);
        }

        entity.PolicyExists = section.PolicyExists;
        entity.DueDiligenceInPlace = section.DueDiligenceInPlace;
        entity.HighRiskAreas = section.HighRiskAreas;
        entity.Remediation = section.Remediation;
        entity.TrainingProvided = section.TrainingProvided;
    }

    private async Task UpsertGovernanceAsync(Guid orgId, Guid periodId, C4GovernanceSection section, CancellationToken ct)
    {
        var entity = await _db.GovernanceOversights.FirstOrDefaultAsync(x => x.ReportingPeriodId == periodId, ct);
        if (entity is null)
        {
            entity = new GovernanceOversight { OrganisationId = orgId, ReportingPeriodId = periodId };
            await _db.GovernanceOversights.AddAsync(entity, ct);
        }

        entity.BoardOversight = section.BoardOversight;
        entity.ManagementResponsibilities = section.ManagementResponsibilities;
        entity.Incentives = section.Incentives;
        entity.ClimateExpertOnBoard = section.ClimateExpertOnBoard;
    }

    private async Task UpsertBoardAsync(Guid orgId, Guid periodId, C5BoardDiversitySection section, CancellationToken ct)
    {
        var entity = await _db.BoardDiversities.FirstOrDefaultAsync(x => x.ReportingPeriodId == periodId, ct);
        if (entity is null)
        {
            entity = new BoardDiversity { OrganisationId = orgId, ReportingPeriodId = periodId };
            await _db.BoardDiversities.AddAsync(entity, ct);
        }

        entity.PercentFemale = section.PercentFemale;
        entity.PercentMale = section.PercentMale;
        entity.PercentOther = section.PercentOther;
        entity.PercentIndependent = section.PercentIndependent;
        entity.DiversityPolicy = section.DiversityPolicy;
        entity.SelectionProcess = section.SelectionProcess;
    }

    private async Task UpsertStakeholderAsync(Guid orgId, Guid periodId, C6StakeholderSection section, CancellationToken ct)
    {
        var entity = await _db.StakeholderEngagements.FirstOrDefaultAsync(x => x.ReportingPeriodId == periodId, ct);
        if (entity is null)
        {
            entity = new StakeholderEngagement { OrganisationId = orgId, ReportingPeriodId = periodId };
            await _db.StakeholderEngagements.AddAsync(entity, ct);
        }

        entity.StakeholderGroups = section.StakeholderGroups;
        entity.EngagementProcess = section.EngagementProcess;
        entity.KeyTopics = section.KeyTopics;
        entity.WorkerRepresentation = section.WorkerRepresentation;
    }

    private async Task UpsertValueChainAsync(Guid orgId, Guid periodId, C7ValueChainSection section, CancellationToken ct)
    {
        var entity = await _db.ValueChainCoverages.FirstOrDefaultAsync(x => x.ReportingPeriodId == periodId, ct);
        if (entity is null)
        {
            entity = new ValueChainCoverage { OrganisationId = orgId, ReportingPeriodId = periodId };
            await _db.ValueChainCoverages.AddAsync(entity, ct);
        }

        entity.UpstreamCoverage = section.UpstreamCoverage;
        entity.DownstreamCoverage = section.DownstreamCoverage;
        entity.Scope3Categories = section.Scope3Categories;
        entity.DataGaps = section.DataGaps;
    }

    private async Task UpsertAssuranceAsync(Guid orgId, Guid periodId, C8AssuranceSection section, CancellationToken ct)
    {
        var entity = await _db.AssuranceActivities.FirstOrDefaultAsync(x => x.ReportingPeriodId == periodId, ct);
        if (entity is null)
        {
            entity = new AssuranceActivity { OrganisationId = orgId, ReportingPeriodId = periodId };
            await _db.AssuranceActivities.AddAsync(entity, ct);
        }

        entity.Provider = section.Provider;
        entity.AssuranceLevel = section.AssuranceLevel;
        entity.Scope = section.Scope;
        entity.AssuranceDate = section.AssuranceDate;
        entity.Summary = section.Summary;
        entity.IsIndependent = section.IsIndependent;
    }

    private async Task UpsertMethodologyAsync(Guid orgId, Guid periodId, C9MethodologySection section, CancellationToken ct)
    {
        var entity = await _db.MethodologyStatements.FirstOrDefaultAsync(x => x.ReportingPeriodId == periodId, ct);
        if (entity is null)
        {
            entity = new MethodologyStatement { OrganisationId = orgId, ReportingPeriodId = periodId };
            await _db.MethodologyStatements.AddAsync(entity, ct);
        }

        entity.ReportingBoundary = section.ReportingBoundary;
        entity.ConsolidationApproach = section.ConsolidationApproach;
        entity.EmissionFactorSources = section.EmissionFactorSources;
        entity.EstimationApproach = section.EstimationApproach;
        entity.MaterialityThreshold = section.MaterialityThreshold;
    }
}
