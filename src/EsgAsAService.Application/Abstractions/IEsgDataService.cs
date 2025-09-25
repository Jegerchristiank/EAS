using EsgAsAService.Application.Models;
using EsgAsAService.Domain.Entities;

namespace EsgAsAService.Application.Abstractions;

public interface IEsgDataService
{
    Task<Company> UpsertCompanyAsync(Company company, CancellationToken ct = default);
    Task<ReportingPeriod> UpsertReportingPeriodAsync(ReportingPeriod period, CancellationToken ct = default);
    Task<ReportingPeriod?> GetReportingPeriodAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<SectionMetricValue>> GetSectionMetricsAsync(Guid reportingPeriodId, CancellationToken ct = default);
    Task SaveSectionMetricsAsync(Guid reportingPeriodId, Guid organisationId, IReadOnlyCollection<SectionMetricValue> values, CancellationToken ct = default);

    // V2 enrichment helpers
    Task<Guid?> GetMappedV2PeriodAsync(Guid v1PeriodId, CancellationToken ct = default);
    Task SavePeriodMappingAsync(Guid v1PeriodId, Guid v2PeriodId, CancellationToken ct = default);
    Task AddWaterAsync(Guid v2PeriodId, double intakeM3, double? dischargeM3, CancellationToken ct = default);
    Task AddWasteAsync(Guid v2PeriodId, string eakCode, double quantityKg, string disposition, CancellationToken ct = default);
    Task AddMaterialAsync(Guid v2PeriodId, string material, double quantityTonnes, CancellationToken ct = default);
    Task UpsertFinancialsAsync(Guid v2PeriodId, double revenue, string? currency, CancellationToken ct = default);
}
