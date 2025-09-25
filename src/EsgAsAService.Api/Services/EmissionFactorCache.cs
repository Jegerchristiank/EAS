using EsgAsAService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EsgAsAService.Api.Services;

/// <summary>
/// Read‑through cache for emission factors by (country, year, type).
/// Why: reduce DB load for hot lookups and provide simple TTL invalidation.
/// </summary>
internal interface IEmissionFactorCache
{
    Task<double?> GetValueAsync(string country, int year, string type, CancellationToken ct = default);
}

internal class EmissionFactorCache : IEmissionFactorCache
{
    private readonly IMemoryCache _cache;
    private readonly EsgDbContext _db;
    public EmissionFactorCache(IMemoryCache cache, EsgDbContext db)
    { _cache = cache; _db = db; }

    public async Task<double?> GetValueAsync(string country, int year, string type, CancellationToken ct = default)
    {
        var key = $"ef:{country}:{year}:{type}";
        if (_cache.TryGetValue<double?>(key, out var val)) return val;
        var factor = await _db.EmissionFactorsV2.AsNoTracking()
            .Where(f => f.Country == country && f.Year == year && f.Type == type)
            .OrderByDescending(f => f.ValidFrom)
            .FirstOrDefaultAsync(ct);
        var value = factor?.Value;
        _cache.Set(key, value, TimeSpan.FromHours(1));
        return value;
    }
}
