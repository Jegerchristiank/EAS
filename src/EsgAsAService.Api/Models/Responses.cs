using System;
using System.Collections.Generic;

namespace EsgAsAService.Api.Models;

/// <summary>
/// Standard container for paginated list responses.
/// </summary>
public record PagedResponse<T>(int Total, int Page, int PageSize, IReadOnlyList<T> Items);

/// <summary>Read model for reference data units.</summary>
public record UnitResponse(Guid Id, string Code, string? Name);

/// <summary>Read model for unit conversions.</summary>
public record UnitConversionResponse(Guid Id, Guid FromUnitId, Guid ToUnitId, double Factor);

/// <summary>Read model for emission factors.</summary>
public record EmissionFactorResponse(Guid Id, string Country, int Year, string Type, Guid UnitId, double Value, DateOnly? ValidFrom, DateOnly? ValidTo, Guid? DataSourceId);

/// <summary>Read model for activities (energy/material entries).</summary>
public record ActivityResponse(Guid Id, Guid OrganisationId, Guid ReportingPeriodId, string Category, DateOnly? ActivityDate, double Quantity, Guid UnitId, string? Description);

/// <summary>Read model for scope entries that tie activities to scopes/emission factors.</summary>
public record ScopeEntryResponse(Guid Id, Guid ActivityId, int Scope, Guid? EmissionFactorId, double? Adjustment);

/// <summary>Read model for deviations applied to calculation results.</summary>
public record DeviationResponse(Guid Id, Guid ScopeEntryId, string Reason);

/// <summary>Read model for data sources.</summary>
public record DataSourceResponse(Guid Id, string Name, string? Description, string? Url);

/// <summary>Read model for locations.</summary>
public record LocationResponse(Guid Id, Guid OrganisationId, string? Name, double? Latitude, double? Longitude, bool InSensitiveArea, string? SensitiveAreaNote);

/// <summary>Read model for water meter entries.</summary>
public record WaterMeterResponse(Guid Id, Guid OrganisationId, Guid ReportingPeriodId, Guid? LocationId, double IntakeM3, double? DischargeM3, string? SourceDoc);

/// <summary>Read model for waste manifests.</summary>
public record WasteManifestResponse(Guid Id, Guid OrganisationId, Guid ReportingPeriodId, Guid? LocationId, string EakCode, double QuantityKg, string Disposition, string? Carrier, string? ManifestId);

/// <summary>Read model for material flows.</summary>
public record MaterialFlowResponse(Guid Id, Guid OrganisationId, Guid ReportingPeriodId, string Material, double QuantityTonnes, string? SourceDoc);

/// <summary>Read model for pollution register entries.</summary>
public record PollutionRegisterResponse(Guid Id, Guid OrganisationId, Guid ReportingPeriodId, Guid? LocationId, string Substance, double Quantity, Guid UnitId, string? ReportingSystem, string? ReportingId);

/// <summary>Read model for governance cases.</summary>
public record GovernanceCaseResponse(Guid Id, Guid OrganisationId, Guid ReportingPeriodId, string Type, string? Outcome, double? Amount, string? CaseRef);

/// <summary>Read model for HR headcount rows.</summary>
public record HRHeadcountResponse(Guid Id, Guid OrganisationId, Guid ReportingPeriodId, double FteTotal, double? FteFemale, double? FteMale, double? FteOther, string? CountryCode);

/// <summary>Read model for HR payroll aggregates.</summary>
public record HRPayrollResponse(Guid Id, Guid OrganisationId, Guid ReportingPeriodId, double? AvgSalaryFemale, double? AvgSalaryMale, double? CollectiveAgreementCoveragePct);

/// <summary>Read model for HR training aggregates.</summary>
public record HRTrainingResponse(Guid Id, Guid OrganisationId, Guid ReportingPeriodId, double? TotalTrainingHours);

/// <summary>Read model for safety incidents.</summary>
public record SafetyIncidentResponse(Guid Id, Guid OrganisationId, Guid ReportingPeriodId, int IncidentsCount, double HoursWorked, string? SourceDoc);

/// <summary>Read model for financial snapshots.</summary>
public record FinancialsResponse(Guid Id, Guid OrganisationId, Guid ReportingPeriodId, double Revenue, string? Currency);

/// <summary>Read model for policy registers.</summary>
public record PolicyRegisterResponse(Guid Id, Guid OrganisationId, bool PolicyClimate, bool PolicyEnvironment, bool PolicyCircular, bool PolicySupplyChain, bool PolicyAntiCorruption, bool PolicyDataPrivacy, bool PolicyWhistleblower, string? Goal, string? Status, string? NextMilestone, bool ConfidentialityOmissions, string? OmissionNote);

/// <summary>Read model for VSME mapping snapshots.</summary>
public record VsmeMappingResponse(Guid Id, string Json, DateTimeOffset CreatedAt);

/// <summary>Read model for report drafts.</summary>
public record ReportDraftResponse(Guid Id, Guid ReportingPeriodId, string Json, DateTimeOffset CreatedAt);
