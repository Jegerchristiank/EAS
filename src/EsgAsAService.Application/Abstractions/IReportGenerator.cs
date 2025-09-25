using EsgAsAService.Domain.Entities;

namespace EsgAsAService.Application.Abstractions;

/// <summary>
/// Generates human-readable and machine-readable ESG reports.
/// Why: keep output formats (PDF/XBRL) behind an interface so
/// controllers and services don’t depend on rendering libraries
/// and we can swap implementations without touching API surface.
/// </summary>
public interface IReportGenerator
{
    /// <summary>
    /// Renders a PDF document for a company + period payload.
    /// </summary>
    Task<byte[]> GeneratePdfAsync(Company company, ReportingPeriod period, CancellationToken ct = default);
    Task<byte[]> GeneratePdfAsync(Models.FullEsgReport report, CancellationToken ct = default);

    /// <summary>
    /// Produces an XBRL (XML) representation for interchange.
    /// </summary>
    Task<byte[]> GenerateXbrlAsync(Company company, ReportingPeriod period, CancellationToken ct = default);
}
