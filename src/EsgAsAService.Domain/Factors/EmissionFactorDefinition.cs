using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace EsgAsAService.Domain.Factors;

/// <summary>
/// Represents a deterministic emission factor definition sourced from an authoritative dataset.
/// </summary>
public sealed record EmissionFactorDefinition(
    string Name,
    string Unit,
    decimal Value,
    string Source,
    string Region,
    int? Year,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    string Methodology)
{
    public string Key => $"{Name}::{Unit}";
}

/// <summary>
/// Describes a factor selected for a specific calculation, including any conversions performed.
/// </summary>
public sealed record FactorSelection(
    EmissionFactorDefinition Definition,
    string SelectedUnit,
    decimal AppliedValue,
    IReadOnlyList<string> ConversionTrace);

/// <summary>
/// Represents the available emission factors for a given version, including custom overrides.
/// </summary>
public sealed record FactorSet(
    string Version,
    IReadOnlyDictionary<string, EmissionFactorDefinition> Auto,
    IReadOnlyDictionary<string, EmissionFactorDefinition> Custom)
{
    public EmissionFactorDefinition Resolve(string key)
    {
        if (TryResolve(key, out var definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Emission factor '{key}' not available in factor set version '{Version}'.");
    }

    public bool TryResolve(string key, [NotNullWhen(true)] out EmissionFactorDefinition? definition)
    {
        if (Custom.TryGetValue(key, out definition))
        {
            return true;
        }

        if (Auto.TryGetValue(key, out definition))
        {
            return true;
        }

        definition = null;
        return false;
    }
}
