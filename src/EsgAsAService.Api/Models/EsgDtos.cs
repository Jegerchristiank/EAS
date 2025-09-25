using System.ComponentModel.DataAnnotations;

namespace EsgAsAService.Api.Models;

public record EmissionFactorRequest(
    [property:Required, MaxLength(2)] string Country,
    [property:Range(1900,3000)] int Year,
    [property:Required, MaxLength(100)] string Type,
    [property:Required] Guid UnitId,
    [property:Range(double.Epsilon, double.MaxValue)] double Value,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    Guid? DataSourceId
);

public record LocationRequest(
    [property:Required] Guid OrganisationId,
    [property:MaxLength(200)] string? Name,
    double? Latitude,
    double? Longitude,
    bool InSensitiveArea,
    [property:MaxLength(500)] string? SensitiveAreaNote);

public record PolicyRegisterRequest(
    [property:Required] Guid OrganisationId,
    bool PolicyClimate,
    bool PolicyEnvironment,
    bool PolicyCircular,
    bool PolicySupplyChain,
    bool PolicyAntiCorruption,
    bool PolicyDataPrivacy,
    bool PolicyWhistleblower,
    [property:MaxLength(1000)] string? Goal,
    [property:MaxLength(500)] string? Status,
    [property:MaxLength(500)] string? NextMilestone,
    bool ConfidentialityOmissions,
    [property:MaxLength(1000)] string? OmissionNote);

public record FinancialsRequest(
    [property:Required] Guid OrganisationId,
    [property:Required] Guid ReportingPeriodId,
    double Revenue,
    [property:MaxLength(3)] string? Currency);

public record WaterMeterRequest(
    [property:Required] Guid OrganisationId,
    [property:Required] Guid ReportingPeriodId,
    Guid? LocationId,
    double IntakeM3,
    double? DischargeM3,
    [property:MaxLength(200)] string? SourceDoc);

public record WasteManifestRequest(
    [property:Required] Guid OrganisationId,
    [property:Required] Guid ReportingPeriodId,
    Guid? LocationId,
    [property:Required, MaxLength(20)] string EakCode,
    double QuantityKg,
    [property:Required, MaxLength(50)] string Disposition,
    [property:MaxLength(200)] string? Carrier,
    [property:MaxLength(100)] string? ManifestId);

public record MaterialFlowRequest(
    [property:Required] Guid OrganisationId,
    [property:Required] Guid ReportingPeriodId,
    [property:Required, MaxLength(200)] string Material,
    double QuantityTonnes,
    [property:MaxLength(200)] string? SourceDoc);

public record HRHeadcountRequest(
    [property:Required] Guid OrganisationId,
    [property:Required] Guid ReportingPeriodId,
    double FteTotal,
    double? FteFemale,
    double? FteMale,
    double? FteOther,
    [property:MaxLength(2)] string? CountryCode);

public record HRPayrollRequest(
    [property:Required] Guid OrganisationId,
    [property:Required] Guid ReportingPeriodId,
    double? AvgSalaryFemale,
    double? AvgSalaryMale,
    double? CollectiveAgreementCoveragePct);

public record HRTrainingRequest(
    [property:Required] Guid OrganisationId,
    [property:Required] Guid ReportingPeriodId,
    double? TotalTrainingHours);

public record SafetyIncidentRequest(
    [property:Required] Guid OrganisationId,
    [property:Required] Guid ReportingPeriodId,
    int IncidentsCount,
    double HoursWorked,
    [property:MaxLength(200)] string? SourceDoc);

public record GovernanceCaseRequest(
    [property:Required] Guid OrganisationId,
    [property:Required] Guid ReportingPeriodId,
    [property:Required, MaxLength(100)] string Type,
    [property:MaxLength(100)] string? Outcome,
    double? Amount,
    [property:MaxLength(100)] string? CaseRef);

public record PollutionRegisterRequest(
    [property:Required] Guid OrganisationId,
    [property:Required] Guid ReportingPeriodId,
    Guid? LocationId,
    [property:Required, MaxLength(200)] string Substance,
    double Quantity,
    [property:Required] Guid UnitId,
    [property:MaxLength(100)] string? ReportingSystem,
    [property:MaxLength(100)] string? ReportingId);
