using EsgAsAService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using EsgAsAService.Api.Models.Vsme;

namespace EsgAsAService.Api.Services;

/// <summary>
/// Builds the VSME “basic” report from stored indicators for a period.
/// Why: keeps aggregation logic separate from controllers and easy to test.
/// </summary>
public interface IVsmeReportService
{
    Task<VsmeBasicReport> GenerateBasicAsync(Guid periodId, CancellationToken ct = default);
}

public class VsmeBasicReportService : IVsmeReportService
{
    private readonly EsgDbContext _db;
    public VsmeBasicReportService(EsgDbContext db) => _db = db;

    public async Task<VsmeBasicReport> GenerateBasicAsync(Guid periodId, CancellationToken ct = default)
    {
        var totals = await _db.CalculationResults
            .Where(r => _db.ScopeEntries.Any(se => se.Id == r.ScopeEntryId && _db.Activities.Any(a => a.Id == se.ActivityId && a.ReportingPeriodId == periodId)))
            .ToListAsync(ct);
        var totalCo2e = totals.Sum(t => t.Co2eKg);
        // TODO: wire up other disclosures to actual indicators when available
        return new VsmeBasicReport(
            new VsmeDisclosure(totalCo2e, new[] { "calculationResults" }),
            new VsmeDisclosure("TBD", new[] { "activities" }),
            new VsmeDisclosure("TBD", new[] { "activities" }),
            new VsmeDisclosure(totals.Where(r => _db.ScopeEntries.Any(se => se.Id == r.ScopeEntryId && se.Scope == 1)).Sum(r => r.Co2eKg), new[] { "calculationResults" }),
            new VsmeDisclosure(totals.Where(r => _db.ScopeEntries.Any(se => se.Id == r.ScopeEntryId && se.Scope == 2)).Sum(r => r.Co2eKg), new[] { "calculationResults" }),
            new VsmeDisclosure(totals.Where(r => _db.ScopeEntries.Any(se => se.Id == r.ScopeEntryId && se.Scope == 3)).Sum(r => r.Co2eKg), new[] { "calculationResults" }),
            new VsmeDisclosure("TBD", new[] { "socialIndicators" }),
            new VsmeDisclosure("TBD", new[] { "socialIndicators" }),
            new VsmeDisclosure("TBD", new[] { "socialIndicators" }),
            new VsmeDisclosure("TBD", new[] { "governance" }),
            new VsmeDisclosure("TBD", new[] { "governance" })
        );
    }
}
