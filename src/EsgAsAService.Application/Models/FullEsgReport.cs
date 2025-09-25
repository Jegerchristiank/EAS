using System.Text.Json.Serialization;

namespace EsgAsAService.Application.Models;

public class FullEsgReport
{
    [JsonPropertyName("meta")] public MetaSection Meta { get; set; } = new();

    [JsonPropertyName("B1")] public B1Section? B1 { get; set; }
    [JsonPropertyName("B2")] public B2Section? B2 { get; set; }
    [JsonPropertyName("B3")] public B3Section? B3 { get; set; }
    [JsonPropertyName("B4")] public List<B4PollutionItem>? B4 { get; set; }
    [JsonPropertyName("B5")] public List<B5BiodiversityItem>? B5 { get; set; }
    [JsonPropertyName("B6")] public B6WaterSection? B6 { get; set; }
    [JsonPropertyName("B7")] public B7ResourcesSection? B7 { get; set; }
    [JsonPropertyName("B8")] public B8WorkforceSection? B8 { get; set; }
    [JsonPropertyName("B9")] public B9SafetySection? B9 { get; set; }
    [JsonPropertyName("B10")] public B10PayTrainingSection? B10 { get; set; }
    [JsonPropertyName("B11")] public List<B11GovernanceCase>? B11 { get; set; }

    [JsonPropertyName("C1")] public C1StrategySection? C1 { get; set; }
    [JsonPropertyName("C2")] public C2RiskSection? C2 { get; set; }
    [JsonPropertyName("C3")] public C3HumanRightsSection? C3 { get; set; }
    [JsonPropertyName("C4")] public C4GovernanceSection? C4 { get; set; }
    [JsonPropertyName("C5")] public C5BoardDiversitySection? C5 { get; set; }
    [JsonPropertyName("C6")] public C6StakeholderSection? C6 { get; set; }
    [JsonPropertyName("C7")] public C7ValueChainSection? C7 { get; set; }
    [JsonPropertyName("C8")] public C8AssuranceSection? C8 { get; set; }
    [JsonPropertyName("C9")] public C9MethodologySection? C9 { get; set; }

    [JsonPropertyName("metrics")] public Dictionary<string, SectionMetricSet> SectionMetrics { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class MetaSection
{
    [JsonPropertyName("period")] public PeriodMeta Period { get; set; } = new();
    [JsonPropertyName("organisation")] public OrganisationMeta Organisation { get; set; } = new();
}

public class PeriodMeta
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

public class OrganisationMeta
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

public class B1Section
{
    [JsonPropertyName("legal_form")] public string? LegalForm { get; set; }
    [JsonPropertyName("nace_code")] public string? NaceCode { get; set; }
    [JsonPropertyName("cvr")] public string? Cvr { get; set; }
    [JsonPropertyName("locations")] public List<B1Location> Locations { get; set; } = new();
    [JsonPropertyName("certificates")] public List<B1Certificate> Certificates { get; set; } = new();
    [JsonPropertyName("confidentiality_omissions")] public bool ConfidentialityOmissions { get; set; }
    [JsonPropertyName("omission_note")] public string? OmissionNote { get; set; }
}

public class B1Location
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    [JsonPropertyName("in_sensitive_area")] public bool InSensitiveArea { get; set; }
    public string? Note { get; set; }
}

public class B1Certificate
{
    public string Standard { get; set; } = string.Empty;
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

public class B2Section
{
    public bool PolicyClimate { get; set; }
    public bool PolicyEnvironment { get; set; }
    public bool PolicyCircular { get; set; }
    public bool PolicySupplyChain { get; set; }
    public bool PolicyAntiCorruption { get; set; }
    public bool PolicyDataPrivacy { get; set; }
    public bool PolicyWhistleblower { get; set; }
    public string? Goal { get; set; }
    public string? Status { get; set; }
    public string? NextMilestone { get; set; }
}

public class B3Section
{
    [JsonPropertyName("scope1_kg")] public double Scope1Kg { get; set; }
    [JsonPropertyName("scope2_kg")] public double Scope2Kg { get; set; }
    [JsonPropertyName("scope3_kg")] public double Scope3Kg { get; set; }
    [JsonPropertyName("total_kg")] public double TotalKg { get; set; }
    [JsonPropertyName("intensity_kg_per_revenue")] public double? IntensityKgPerRevenue { get; set; }
    [JsonPropertyName("by_carrier")] public List<B3CarrierItem> ByCarrier { get; set; } = new();
}

public class B3CarrierItem
{
    [JsonPropertyName("carrier")] public string Carrier { get; set; } = string.Empty;
    [JsonPropertyName("co2e_kg")] public double Co2eKg { get; set; }
}

public class B4PollutionItem
{
    public string Substance { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public Guid UnitId { get; set; }
    [JsonPropertyName("unit_code")] public string? UnitCode { get; set; }
    public string? ReportingSystem { get; set; }
    public string? ReportingId { get; set; }
}

public class B5BiodiversityItem
{
    public Guid Id { get; set; }
    public bool InSensitiveArea { get; set; }
    public string? SensitiveAreaNote { get; set; }
}

public class B6WaterSection
{
    [JsonPropertyName("intake_m3")] public double IntakeM3 { get; set; }
    [JsonPropertyName("discharge_m3")] public double DischargeM3 { get; set; }
    [JsonPropertyName("consumption_m3")] public double ConsumptionM3 { get; set; }
}

public class B7ResourcesSection
{
    public List<B7WasteItem> Waste { get; set; } = new();
    public List<B7MaterialItem> Materials { get; set; } = new();
}

public class B7WasteItem
{
    [JsonPropertyName("eak_code")] public string EakCode { get; set; } = string.Empty;
    [JsonPropertyName("qty_kg")] public double QuantityKg { get; set; }
    public string Disposition { get; set; } = string.Empty;
}

public class B7MaterialItem
{
    public string Material { get; set; } = string.Empty;
    [JsonPropertyName("qty_tonnes")] public double QuantityTonnes { get; set; }
}

public class B8WorkforceSection
{
    [JsonPropertyName("fte_total")] public double FteTotal { get; set; }
    public List<object>? Headcount { get; set; }
    [JsonPropertyName("turnover_rate")] public double? TurnoverRate { get; set; }
}

public class B9SafetySection
{
    [JsonPropertyName("incidents_count")] public double IncidentsCount { get; set; }
    [JsonPropertyName("hours_worked")] public double HoursWorked { get; set; }
    [JsonPropertyName("accident_frequency")] public double? AccidentFrequency { get; set; }
}

public class B10PayTrainingSection
{
    [JsonPropertyName("gender_pay_gap")] public double? GenderPayGap { get; set; }
    [JsonPropertyName("coverage_pct")] public double? CoveragePct { get; set; }
    [JsonPropertyName("training_hours_per_employee")] public double? TrainingHoursPerEmployee { get; set; }
}

public class B11GovernanceCase
{
    public string Type { get; set; } = string.Empty;
    public string? Outcome { get; set; }
    public double? Amount { get; set; }
    public string? CaseRef { get; set; }
}

public class C1StrategySection
{
    public string? Summary { get; set; }
    [JsonPropertyName("short_term_target")] public string? ShortTermTarget { get; set; }
    [JsonPropertyName("long_term_target")] public string? LongTermTarget { get; set; }
    [JsonPropertyName("emission_reduction_target_pct")] public double? EmissionReductionTargetPct { get; set; }
    [JsonPropertyName("target_year")] public int? TargetYear { get; set; }
    [JsonPropertyName("investment_plan")] public string? InvestmentPlan { get; set; }
    public string? Progress { get; set; }
}

public class C2RiskSection
{
    public string? Process { get; set; }
    [JsonPropertyName("climate_risks")] public string? ClimateRisks { get; set; }
    public string? Opportunities { get; set; }
    [JsonPropertyName("time_horizon")] public string? TimeHorizon { get; set; }
    public string? Mitigations { get; set; }
}

public class C3HumanRightsSection
{
    [JsonPropertyName("policy_exists")] public bool PolicyExists { get; set; }
    [JsonPropertyName("due_diligence_in_place")] public bool DueDiligenceInPlace { get; set; }
    [JsonPropertyName("high_risk_areas")] public string? HighRiskAreas { get; set; }
    public string? Remediation { get; set; }
    [JsonPropertyName("training_provided")] public string? TrainingProvided { get; set; }
}

public class C4GovernanceSection
{
    [JsonPropertyName("board_oversight")] public string? BoardOversight { get; set; }
    [JsonPropertyName("management_responsibilities")] public string? ManagementResponsibilities { get; set; }
    public string? Incentives { get; set; }
    [JsonPropertyName("climate_expert_on_board")] public bool? ClimateExpertOnBoard { get; set; }
}

public class C5BoardDiversitySection
{
    [JsonPropertyName("percent_female")] public double? PercentFemale { get; set; }
    [JsonPropertyName("percent_male")] public double? PercentMale { get; set; }
    [JsonPropertyName("percent_other")] public double? PercentOther { get; set; }
    [JsonPropertyName("percent_independent")] public double? PercentIndependent { get; set; }
    [JsonPropertyName("diversity_policy")] public string? DiversityPolicy { get; set; }
    [JsonPropertyName("selection_process")] public string? SelectionProcess { get; set; }
}

public class C6StakeholderSection
{
    [JsonPropertyName("stakeholder_groups")] public string? StakeholderGroups { get; set; }
    [JsonPropertyName("engagement_process")] public string? EngagementProcess { get; set; }
    [JsonPropertyName("key_topics")] public string? KeyTopics { get; set; }
    [JsonPropertyName("worker_representation")] public bool? WorkerRepresentation { get; set; }
}

public class C7ValueChainSection
{
    [JsonPropertyName("upstream_coverage")] public string? UpstreamCoverage { get; set; }
    [JsonPropertyName("downstream_coverage")] public string? DownstreamCoverage { get; set; }
    [JsonPropertyName("scope3_categories")] public string? Scope3Categories { get; set; }
    [JsonPropertyName("data_gaps")] public string? DataGaps { get; set; }
}

public class C8AssuranceSection
{
    public string? Provider { get; set; }
    [JsonPropertyName("assurance_level")] public string? AssuranceLevel { get; set; }
    public string? Scope { get; set; }
    [JsonPropertyName("assurance_date")] public DateOnly? AssuranceDate { get; set; }
    public string? Summary { get; set; }
    [JsonPropertyName("is_independent")] public bool? IsIndependent { get; set; }
}

public class C9MethodologySection
{
    [JsonPropertyName("reporting_boundary")] public string? ReportingBoundary { get; set; }
    [JsonPropertyName("consolidation_approach")] public string? ConsolidationApproach { get; set; }
    [JsonPropertyName("emission_factor_sources")] public string? EmissionFactorSources { get; set; }
    [JsonPropertyName("estimation_approach")] public string? EstimationApproach { get; set; }
    [JsonPropertyName("materiality_threshold")] public string? MaterialityThreshold { get; set; }
}
