using EsgAsAService.Application.Abstractions;
using EsgAsAService.Domain.Entities;

namespace EsgAsAService.Infrastructure.EmissionFactors;

/// <summary>
/// Tries multiple providers in order until a factor is found.
/// </summary>
public class CompositeEmissionFactorProvider : IEmissionFactorProvider
{
    private readonly IEnumerable<IEmissionFactorProvider> _providers;
    public CompositeEmissionFactorProvider(IEnumerable<IEmissionFactorProvider> providers)
    {
        _providers = providers;
    }

    public async Task<EmissionFactor?> GetFactorAsync(string category, string unit, DateOnly? onDate = null, CancellationToken ct = default)
    {
        foreach (var p in _providers)
        {
            var f = await p.GetFactorAsync(category, unit, onDate, ct);
            if (f is not null) return f;
        }
        return null;
    }
}

