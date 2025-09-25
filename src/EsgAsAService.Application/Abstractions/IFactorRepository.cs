using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EsgAsAService.Domain.Factors;

namespace EsgAsAService.Application.Abstractions;

/// <summary>
/// Provides access to auto and custom emission factor datasets.
/// </summary>
public interface IFactorRepository
{
    Task<FactorSet> LoadFactorSetAsync(string version, CancellationToken cancellationToken = default);
    Task SaveCustomFactorsAsync(string version, IEnumerable<EmissionFactorDefinition> factors, CancellationToken cancellationToken = default);
}
