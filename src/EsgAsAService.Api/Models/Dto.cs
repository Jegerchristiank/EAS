using System.ComponentModel.DataAnnotations;

namespace EsgAsAService.Api.Models;

public record IdResponse(Guid Id);

/// <summary>
/// Request DTO for creating or updating an organisation.
/// Why: keep public API stable and avoid leaking persistence fields.
/// </summary>
public record OrganisationRequest(
    [property:Required, MaxLength(200)] string Name,
    [property:MaxLength(100)] string? Industry,
    [property:MaxLength(2)] string? CountryCode,
    [property:MaxLength(100)] string? OrganizationNumber
);

/// <summary>
/// Request DTO for creating or updating a reporting period (V2).
/// </summary>
public record ReportingPeriodRequest(
    [property:Required] Guid OrganisationId,
    [property:Range(1900,3000)] int Year,
    DateOnly StartDate,
    DateOnly EndDate
);

/// <summary>
/// Request DTO for adding an activity to a reporting period.
/// Category examples: Fuel/Diesel, Electricity, DistrictHeating.
/// </summary>
public record ActivityRequest(
    [property:Required] Guid OrganisationId,
    [property:Required] Guid ReportingPeriodId,
    [property:Required, MaxLength(200)] string Category,
    DateOnly? ActivityDate,
    [property:Range(0,double.MaxValue)] double Quantity,
    [property:Required] Guid UnitId
);

public record ScopeEntryRequest(
    [property:Required] Guid ActivityId,
    [property:Range(1,3)] int Scope,
    Guid? EmissionFactorId,
    double? Adjustment
);

public record DeviationRequest(
    [property:Required] Guid ScopeEntryId,
    [property:Required, MaxLength(500)] string Reason
);

public record EvidenceUploadRequest([property:Required] Guid ScopeEntryId, [property:Required, MaxLength(300)] string FileName);
public record EvidenceUploadResponse(Uri UploadUrl, string Token);

public record ApprovalSubmitRequest([property:Required] Guid ReportingPeriodId);
public record ApprovalPatchRequest([property:Required, MaxLength(50)] string Status, [property:MaxLength(1000)] string? Comment);

public record EmissionFactorQuery(string Country, int Year, string Type);

public record UnitRequest(
    [property:Required, MaxLength(32)] string Code,
    [property:MaxLength(128)] string? Name
);

public record UnitConversionRequest(
    [property:Required] Guid FromUnitId,
    [property:Required] Guid ToUnitId,
    [property:Range(double.Epsilon, double.MaxValue)] double Factor
);
