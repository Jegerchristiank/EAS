using EsgAsAService.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Services;

/// <summary>
/// Executes deterministic calculations for a period’s scope entries,
/// enforcing preconditions (evidence or deviation) before computing CO2e.
/// </summary>
public interface ICalculationRunner
{
    Task<int> RunAsync(Guid periodId, CancellationToken ct = default);
}

public class CalculationRunner : ICalculationRunner
{
    private readonly EsgAsAService.Infrastructure.Data.EsgDbContext _db;
    private readonly IUnitConversionService _units;
    public CalculationRunner(EsgAsAService.Infrastructure.Data.EsgDbContext db, IUnitConversionService units)
    { _db = db; _units = units; }

    public async Task<int> RunAsync(Guid periodId, CancellationToken ct = default)
    {
        // Precondition: at least one evidence per scope entry OR a Deviation exists
        var entries = await _db.ScopeEntries
            .Where(se => _db.Activities.Any(a => a.Id == se.ActivityId && a.ReportingPeriodId == periodId))
            .ToListAsync(ct);

        var count = 0;
        foreach (var se in entries)
        {
            var evidences = await _db.EvidenceDocuments.Where(e => e.ScopeEntryId == se.Id).AnyAsync(ct);
            var hasDeviation = await _db.Deviations.Where(d => d.ScopeEntryId == se.Id).AnyAsync(ct);
            if (!evidences && !hasDeviation)
            {
                throw new InvalidOperationException("Evidence required or deviation must be recorded for each scope entry.");
            }

            var activity = await _db.Activities.FirstAsync(a => a.Id == se.ActivityId, ct);

            // Normalize quantity to the factor's unit, if factor is set
            double quantityNormalized = activity.Quantity;
            double factorValue = 0; Guid? factorUnitId = null;
            if (se.EmissionFactorId.HasValue)
            {
                var factor = await _db.EmissionFactorsV2.FirstAsync(f => f.Id == se.EmissionFactorId.Value, ct);
                factorValue = factor.Value;
                factorUnitId = factor.UnitId;
            }

            if (factorUnitId.HasValue)
            {
                quantityNormalized = await _units.ConvertAsync(activity.UnitId, factorUnitId.Value, activity.Quantity, ct);
            }

            var adjustment = se.Adjustment ?? 1.0;
            var co2e = quantityNormalized * factorValue * adjustment;

            var result = new CalculationResult
            {
                ScopeEntryId = se.Id,
                QuantityNormalized = quantityNormalized,
                Factor = factorValue,
                Adjustment = adjustment,
                Co2eKg = co2e
            };
            _db.Add(result);
            count++;
        }

        await _db.SaveChangesAsync(ct);
        return count;
    }
}
