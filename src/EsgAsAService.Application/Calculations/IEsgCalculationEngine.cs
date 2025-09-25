using EsgAsAService.Domain.Calculations;
using EsgAsAService.Domain.Factors;
using EsgAsAService.Domain.Input;

namespace EsgAsAService.Application.Calculations;

/// <summary>
/// Contract for orchestrating the full ESRS module calculation pipeline.
/// </summary>
public interface IEsgCalculationEngine
{
    Task<ModuleCalculationEnvelope> RunAllModulesAsync(
        EsgInputPayload input,
        FactorSet factors,
        CalculationAssumptions assumptions,
        CancellationToken cancellationToken = default);
}
