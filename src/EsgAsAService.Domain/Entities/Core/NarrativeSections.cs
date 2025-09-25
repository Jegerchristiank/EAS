using System.ComponentModel.DataAnnotations;

namespace EsgAsAService.Domain.Entities.Core;

public class StrategyTarget : RevisionedEntity
{
    [Required] public Guid OrganisationId { get; set; }
    [Required] public Guid ReportingPeriodId { get; set; }

    [MaxLength(1000)] public string? Summary { get; set; }
    [MaxLength(500)] public string? ShortTermTarget { get; set; }
    [MaxLength(500)] public string? LongTermTarget { get; set; }
    public double? EmissionReductionTargetPct { get; set; }
    public int? TargetYear { get; set; }
    [MaxLength(500)] public string? InvestmentPlan { get; set; }
    [MaxLength(500)] public string? Progress { get; set; }
}

public class RiskAssessment : RevisionedEntity
{
    [Required] public Guid OrganisationId { get; set; }
    [Required] public Guid ReportingPeriodId { get; set; }

    [MaxLength(1000)] public string? Process { get; set; }
    [MaxLength(1000)] public string? ClimateRisks { get; set; }
    [MaxLength(1000)] public string? Opportunities { get; set; }
    [MaxLength(200)] public string? TimeHorizon { get; set; }
    [MaxLength(1000)] public string? Mitigations { get; set; }
}

public class HumanRightsAssessment : RevisionedEntity
{
    [Required] public Guid OrganisationId { get; set; }
    [Required] public Guid ReportingPeriodId { get; set; }

    public bool PolicyExists { get; set; }
    public bool DueDiligenceInPlace { get; set; }
    [MaxLength(1000)] public string? HighRiskAreas { get; set; }
    [MaxLength(1000)] public string? Remediation { get; set; }
    [MaxLength(500)] public string? TrainingProvided { get; set; }
}

public class GovernanceOversight : RevisionedEntity
{
    [Required] public Guid OrganisationId { get; set; }
    [Required] public Guid ReportingPeriodId { get; set; }

    [MaxLength(1000)] public string? BoardOversight { get; set; }
    [MaxLength(1000)] public string? ManagementResponsibilities { get; set; }
    [MaxLength(1000)] public string? Incentives { get; set; }
    public bool? ClimateExpertOnBoard { get; set; }
}

public class BoardDiversity : RevisionedEntity
{
    [Required] public Guid OrganisationId { get; set; }
    [Required] public Guid ReportingPeriodId { get; set; }

    public double? PercentFemale { get; set; }
    public double? PercentMale { get; set; }
    public double? PercentOther { get; set; }
    public double? PercentIndependent { get; set; }
    [MaxLength(500)] public string? DiversityPolicy { get; set; }
    [MaxLength(500)] public string? SelectionProcess { get; set; }
}

public class StakeholderEngagement : RevisionedEntity
{
    [Required] public Guid OrganisationId { get; set; }
    [Required] public Guid ReportingPeriodId { get; set; }

    [MaxLength(1000)] public string? StakeholderGroups { get; set; }
    [MaxLength(1000)] public string? EngagementProcess { get; set; }
    [MaxLength(1000)] public string? KeyTopics { get; set; }
    public bool? WorkerRepresentation { get; set; }
}

public class ValueChainCoverage : RevisionedEntity
{
    [Required] public Guid OrganisationId { get; set; }
    [Required] public Guid ReportingPeriodId { get; set; }

    [MaxLength(1000)] public string? UpstreamCoverage { get; set; }
    [MaxLength(1000)] public string? DownstreamCoverage { get; set; }
    [MaxLength(1000)] public string? Scope3Categories { get; set; }
    [MaxLength(1000)] public string? DataGaps { get; set; }
}

public class AssuranceActivity : RevisionedEntity
{
    [Required] public Guid OrganisationId { get; set; }
    [Required] public Guid ReportingPeriodId { get; set; }

    [MaxLength(200)] public string? Provider { get; set; }
    [MaxLength(200)] public string? AssuranceLevel { get; set; }
    [MaxLength(500)] public string? Scope { get; set; }
    public DateOnly? AssuranceDate { get; set; }
    [MaxLength(1000)] public string? Summary { get; set; }
    public bool? IsIndependent { get; set; }
}

public class MethodologyStatement : RevisionedEntity
{
    [Required] public Guid OrganisationId { get; set; }
    [Required] public Guid ReportingPeriodId { get; set; }

    [MaxLength(1000)] public string? ReportingBoundary { get; set; }
    [MaxLength(1000)] public string? ConsolidationApproach { get; set; }
    [MaxLength(1000)] public string? EmissionFactorSources { get; set; }
    [MaxLength(1000)] public string? EstimationApproach { get; set; }
    [MaxLength(500)] public string? MaterialityThreshold { get; set; }
}
