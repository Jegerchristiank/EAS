using System;
using System.Collections.Generic;

namespace EsgAsAService.Domain.Calculations;

/// <summary>
/// Represents the deterministic output of a single ESRS module calculation.
/// </summary>
public sealed record ModuleResult(
    string Module,
    string SectionCode,
    decimal ValueRaw,
    string Unit,
    string Method,
    IReadOnlyList<string> Sources,
    string Trace,
    decimal? ValueRounded,
    IReadOnlyList<string> Warnings)
{
    public static ModuleResult Empty(string module, string sectionCode, string unit) => new(
        module,
        sectionCode,
        0m,
        unit,
        "not_calculated",
        Array.Empty<string>(),
        "",
        null,
        Array.Empty<string>());
}

/// <summary>
/// Envelope aggregating all module results plus diagnostics.
/// </summary>
public sealed record ModuleCalculationEnvelope(
    IReadOnlyDictionary<string, ModuleResult> Results,
    IReadOnlyList<string> GlobalWarnings,
    IReadOnlyList<string> Errors)
{
    public bool HasErrors => Errors.Count > 0;

    public ModuleResult GetResult(string sectionCode)
    {
        if (!Results.TryGetValue(sectionCode, out var result))
        {
            throw new KeyNotFoundException($"Result for section '{sectionCode}' not found.");
        }
        return result;
    }
}
