using System.Collections.Generic;

namespace EsgAsAService.Domain.Calculations;

/// <summary>
/// Captures explicit assumptions supplied by the user or defaulted by the system.
/// </summary>
public sealed record CalculationAssumptions(
    IReadOnlyDictionary<string, string> Values)
{
    public string? Get(string key) => Values.TryGetValue(key, out var value) ? value : null;
}
