using EsgAsAService.Domain.Entities;

namespace EsgAsAService.Application.Abstractions;

/// <summary>
/// Provides emission factors from external sources (e.g., Klimakompasset, EFRAG).
/// </summary>
public interface IEmissionFactorProvider
{
    Task<EmissionFactor?> GetFactorAsync(string category, string unit, DateOnly? onDate = null, CancellationToken ct = default);
}

