using EsgAsAService.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Services;

/// <summary>
/// Converts quantities between units using UnitConversion rows.
/// Why: normalize inputs early so downstream calculations stay simple.
/// </summary>
public interface IUnitConversionService
{
    Task<double> ConvertAsync(Guid fromUnitId, Guid toUnitId, double value, CancellationToken ct = default);
}

public class UnitConversionService : IUnitConversionService
{
    private readonly EsgAsAService.Infrastructure.Data.EsgDbContext _db;
    public UnitConversionService(EsgAsAService.Infrastructure.Data.EsgDbContext db) => _db = db;

    public async Task<double> ConvertAsync(Guid fromUnitId, Guid toUnitId, double value, CancellationToken ct = default)
    {
        if (fromUnitId == toUnitId) return value;
        var conv = await _db.UnitConversions
            .Where(c => c.FromUnitId == fromUnitId && c.ToUnitId == toUnitId)
            .FirstOrDefaultAsync(ct);
        if (conv == null)
        {
            throw new InvalidOperationException("Missing unit conversion");
        }
        return value * conv.Factor;
    }
}
