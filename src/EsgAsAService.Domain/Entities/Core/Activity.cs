using System.ComponentModel.DataAnnotations;

namespace EsgAsAService.Domain.Entities.Core;

public class Activity : RevisionedEntity
{
    [Required]
    public Guid OrganisationId { get; set; }

    [Required]
    public Guid ReportingPeriodId { get; set; }

    [Required, MaxLength(200)]
    public string Category { get; set; } = string.Empty; // e.g., electricity, diesel, freight

    public DateOnly? ActivityDate { get; set; }

    [Range(0, double.MaxValue)]
    public double Quantity { get; set; }

    [Required]
    public Guid UnitId { get; set; }

    [MaxLength(300)]
    public string? Description { get; set; }
}

public class ScopeEntry : RevisionedEntity
{
    [Required]
    public Guid ActivityId { get; set; }

    [Range(1,3)]
    public int Scope { get; set; } // 1,2,3

    public Guid? EmissionFactorId { get; set; }

    /// <summary>
    /// Optional adjustment multiplier (market-based, RECs etc)
    /// </summary>
    public double? Adjustment { get; set; }
}

public class EvidenceDocument : RevisionedEntity
{
    [Required]
    public Guid ScopeEntryId { get; set; }

    [Required, MaxLength(300)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string BlobUrl { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class CalculationResult : RevisionedEntity
{
    [Required]
    public Guid ScopeEntryId { get; set; }

    public double QuantityNormalized { get; set; }
    public double Factor { get; set; }
    public double Adjustment { get; set; } = 1.0;
    public double Co2eKg { get; set; }
}

public class Deviation : RevisionedEntity
{
    [Required]
    public Guid ScopeEntryId { get; set; }

    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class Approval : RevisionedEntity
{
    [Required]
    public Guid ReportingPeriodId { get; set; }

    [Required, MaxLength(50)]
    public string Status { get; set; } = "Draft"; // Draft, Under review, Approved, Rejected

    [MaxLength(1000)]
    public string? Comment { get; set; }
}

public class ReportDraft : RevisionedEntity
{
    [Required]
    public Guid ReportingPeriodId { get; set; }

    [Required]
    public string Json { get; set; } = "{}";
}

public class VsmeMapping : RevisionedEntity
{
    [Required]
    public string Json { get; set; } = "{}"; // mapping rules for disclosures
}

public class AuditLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    [MaxLength(256)] public string? UserId { get; set; }
    [Required, MaxLength(200)] public string EntityName { get; set; } = string.Empty;
    [Required] public Guid EntityId { get; set; }
    [Required, MaxLength(50)] public string Action { get; set; } = string.Empty; // Insert/Update
    [Required, MaxLength(128)] public string PayloadHash { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
}

