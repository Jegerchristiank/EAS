using System.ComponentModel.DataAnnotations;

namespace EsgAsAService.Domain.Entities.Core;

public class Location : RevisionedEntity
{
    [Required]
    public Guid OrganisationId { get; set; }

    [MaxLength(200)]
    public string? Name { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public bool InSensitiveArea { get; set; }
    [MaxLength(500)]
    public string? SensitiveAreaNote { get; set; }
}

public class Certificate : RevisionedEntity
{
    [Required]
    public Guid OrganisationId { get; set; }

    [Required, MaxLength(100)]
    public string Standard { get; set; } = string.Empty;

    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

public class PolicyRegister : RevisionedEntity
{
    [Required]
    public Guid OrganisationId { get; set; }

    public bool PolicyClimate { get; set; }
    public bool PolicyEnvironment { get; set; }
    public bool PolicyCircular { get; set; }
    public bool PolicySupplyChain { get; set; }
    public bool PolicyAntiCorruption { get; set; }
    public bool PolicyDataPrivacy { get; set; }
    public bool PolicyWhistleblower { get; set; }

    [MaxLength(1000)] public string? Goal { get; set; }
    [MaxLength(500)] public string? Status { get; set; }
    [MaxLength(500)] public string? NextMilestone { get; set; }

    public bool ConfidentialityOmissions { get; set; }
    [MaxLength(1000)] public string? OmissionNote { get; set; }
}

public class WaterMeter : RevisionedEntity
{
    [Required] public Guid OrganisationId { get; set; }
    [Required] public Guid ReportingPeriodId { get; set; }
    public Guid? LocationId { get; set; }

    public double IntakeM3 { get; set; }
    public double? DischargeM3 { get; set; }

    [MaxLength(200)] public string? SourceDoc { get; set; }
}

public class WasteManifest : RevisionedEntity
{
    [Required] public Guid OrganisationId { get; set; }
    [Required] public Guid ReportingPeriodId { get; set; }
    public Guid? LocationId { get; set; }

    [Required, MaxLength(20)] public string EakCode { get; set; } = string.Empty;
    public double QuantityKg { get; set; }
    [Required, MaxLength(50)] public string Disposition { get; set; } = string.Empty; // recycle, reuse_prep, disposal
    [MaxLength(200)] public string? Carrier { get; set; }
    [MaxLength(100)] public string? ManifestId { get; set; }
}

public class MaterialFlow : RevisionedEntity
{
    [Required] public Guid OrganisationId { get; set; }
    [Required] public Guid ReportingPeriodId { get; set; }

    [Required, MaxLength(200)] public string Material { get; set; } = string.Empty;
    public double QuantityTonnes { get; set; }
    [MaxLength(200)] public string? SourceDoc { get; set; }
}

public class HRHeadcount : RevisionedEntity
{
    [Required] public Guid OrganisationId { get; set; }
    [Required] public Guid ReportingPeriodId { get; set; }

    public double FteTotal { get; set; }
    public double? FteFemale { get; set; }
    public double? FteMale { get; set; }
    public double? FteOther { get; set; }
    [MaxLength(2)] public string? CountryCode { get; set; }
}

public class HRPayroll : RevisionedEntity
{
    [Required] public Guid OrganisationId { get; set; }
    [Required] public Guid ReportingPeriodId { get; set; }

    public double? AvgSalaryFemale { get; set; }
    public double? AvgSalaryMale { get; set; }
    public double? CollectiveAgreementCoveragePct { get; set; }
}

public class HRTraining : RevisionedEntity
{
    [Required] public Guid OrganisationId { get; set; }
    [Required] public Guid ReportingPeriodId { get; set; }

    public double? TotalTrainingHours { get; set; }
}

public class SafetyIncident : RevisionedEntity
{
    [Required] public Guid OrganisationId { get; set; }
    [Required] public Guid ReportingPeriodId { get; set; }

    public int IncidentsCount { get; set; }
    public double HoursWorked { get; set; }
    [MaxLength(200)] public string? SourceDoc { get; set; }
}

public class GovernanceCase : RevisionedEntity
{
    [Required] public Guid OrganisationId { get; set; }
    [Required] public Guid ReportingPeriodId { get; set; }

    [Required, MaxLength(100)] public string Type { get; set; } = string.Empty; // corruption, bribery, etc.
    [MaxLength(100)] public string? Outcome { get; set; }
    public double? Amount { get; set; }
    [MaxLength(100)] public string? CaseRef { get; set; }
}

public class Financials : RevisionedEntity
{
    [Required] public Guid OrganisationId { get; set; }
    [Required] public Guid ReportingPeriodId { get; set; }

    public double Revenue { get; set; }
    [MaxLength(3)] public string? Currency { get; set; }
}

public class PollutionRegister : RevisionedEntity
{
    [Required] public Guid OrganisationId { get; set; }
    [Required] public Guid ReportingPeriodId { get; set; }
    public Guid? LocationId { get; set; }

    [Required, MaxLength(200)] public string Substance { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public Guid UnitId { get; set; }
    [MaxLength(100)] public string? ReportingSystem { get; set; }
    [MaxLength(100)] public string? ReportingId { get; set; }
}

public class StagingDocument : RevisionedEntity
{
    [Required] public Guid OrganisationId { get; set; }
    public Guid? ReportingPeriodId { get; set; }

    [Required, MaxLength(50)] public string Type { get; set; } = string.Empty; // energy_invoice, water_invoice, waste_manifest, hr_headcount, payroll, training, financials
    [MaxLength(100)] public string? Source { get; set; }
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? PayloadJson { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    [MaxLength(50)] public string? Status { get; set; }
    [MaxLength(1000)] public string? ErrorMessage { get; set; }
}

public class StagingLine : RevisionedEntity
{
    [Required] public Guid DocumentId { get; set; }
    public int LineNo { get; set; }
    public string Json { get; set; } = "{}"; // normalized per parser
}

public class PeriodMapping
{
    // Explicit mapping between V1 and V2 periods for deterministic enrichment
    [Key]
    public Guid V1PeriodId { get; set; }
    [Required]
    public Guid V2PeriodId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
