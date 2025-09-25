using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EsgAsAService.Domain.Calculations;
using EsgAsAService.Domain.Factors;
using EsgAsAService.Domain.Input;
using Microsoft.Extensions.Logging;

namespace EsgAsAService.Application.Calculations;

/// <summary>
/// Default functional-core orchestrator that executes module calculations in a deterministic order.
/// </summary>
public sealed class ModuleCalculationEngine : IEsgCalculationEngine
{
    private readonly ILogger<ModuleCalculationEngine> _logger;
    private readonly ModuleFunctionRegistry _registry;

    public ModuleCalculationEngine(ILogger<ModuleCalculationEngine> logger, ModuleFunctionRegistry registry)
    {
        _logger = logger;
        _registry = registry;
    }

    public Task<ModuleCalculationEnvelope> RunAllModulesAsync(
        EsgInputPayload input,
        FactorSet factors,
        CalculationAssumptions assumptions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(factors);
        ArgumentNullException.ThrowIfNull(assumptions);

        var results = new Dictionary<string, ModuleResult>(StringComparer.OrdinalIgnoreCase);
        var warnings = new List<string>();
        var errors = new List<string>();

        foreach (var module in _registry.OrderedModules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var context = new ModuleCalculationContext(input, factors, assumptions, results);
                var result = module.Invoke(context);
                results[result.SectionCode] = result;
                if (result.Warnings.Count > 0)
                {
                    warnings.AddRange(result.Warnings.Select(w => $"{result.SectionCode}: {w}"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate module {Section}", module.SectionCode);
                errors.Add($"{module.SectionCode}: {ex.Message}");
            }
        }

        return Task.FromResult(new ModuleCalculationEnvelope(results, warnings, errors));
    }
}

/// <summary>
/// Runtime context passed to each module calculation function.
/// </summary>
public sealed class ModuleCalculationContext(
    EsgInputPayload Input,
    FactorSet Factors,
    CalculationAssumptions Assumptions,
    IReadOnlyDictionary<string, ModuleResult> PriorResults)
{
    public EsgInputPayload Input { get; } = Input;
    public FactorSet Factors { get; } = Factors;
    public CalculationAssumptions Assumptions { get; } = Assumptions;
    public IReadOnlyDictionary<string, ModuleResult> PriorResults { get; } = PriorResults;
}

/// <summary>
/// Internal descriptor for a module function with deterministic ordering.
/// </summary>
public sealed record ModuleFunctionDescriptor(
    string Module,
    string SectionCode,
    Func<ModuleCalculationContext, ModuleResult> Invoke);

/// <summary>
/// Registry for module calculation delegates; keeps execution order and resolves lookups.
/// </summary>
public sealed class ModuleFunctionRegistry
{
    private readonly IReadOnlyList<ModuleFunctionDescriptor> _modules;
    private readonly IReadOnlyDictionary<string, ModuleFunctionDescriptor> _lookup;

    public ModuleFunctionRegistry(IEnumerable<ModuleFunctionDescriptor> modules)
    {
        _modules = modules
            .OrderBy(m => m.SectionCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        _lookup = _modules.ToDictionary(m => m.SectionCode, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ModuleFunctionDescriptor> OrderedModules => _modules;

    public ModuleFunctionDescriptor Get(string sectionCode)
        => _lookup.TryGetValue(sectionCode, out var descriptor)
            ? descriptor
            : throw new KeyNotFoundException($"Module function '{sectionCode}' is not registered.");
}
