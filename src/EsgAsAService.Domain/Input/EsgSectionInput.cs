using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EsgAsAService.Domain.Input;

/// <summary>
/// Represents a logical ESG section populated with validated field values.
/// </summary>
public sealed record EsgSectionInput(
    string SectionCode,
    string SectionName,
    string Module,
    IReadOnlyDictionary<string, EsgFieldValue> Fields)
{
    public EsgFieldValue GetFieldOrDefault(string datapointKey)
        => Fields.TryGetValue(datapointKey, out var value) ? value : EsgFieldValue.Empty;

    public bool TryGetDecimal(string datapointKey, out decimal value)
    {
        if (Fields.TryGetValue(datapointKey, out var field) && field.TryGetDecimal(out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    public decimal GetDecimal(string datapointKey, decimal defaultValue = 0m)
        => TryGetDecimal(datapointKey, out var value) ? value : defaultValue;

    public bool TryGetBoolean(string datapointKey, out bool value)
    {
        if (Fields.TryGetValue(datapointKey, out var field) && field.TryGetBoolean(out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    public bool GetBoolean(string datapointKey, bool defaultValue = false)
        => TryGetBoolean(datapointKey, out var value) ? value : defaultValue;

    public string GetString(string datapointKey, string defaultValue = "")
        => Fields.TryGetValue(datapointKey, out var field) && field.TryGetString(out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : defaultValue;

    public int GetArrayLength(string datapointKey)
    {
        if (!Fields.TryGetValue(datapointKey, out var field))
        {
            return 0;
        }

        return field switch
        {
            EsgFieldValue.StringArray array => array.Values.Count,
            EsgFieldValue.ObjectArray objects => objects.Values.Count,
            _ => 0
        };
    }

    public bool HasValue(string datapointKey)
    {
        if (!Fields.TryGetValue(datapointKey, out var field))
        {
            return false;
        }

        return field switch
        {
            EsgFieldValue.Text text => !string.IsNullOrWhiteSpace(text.Value),
            EsgFieldValue.StringArray array => array.Values.Count > 0,
            EsgFieldValue.ObjectArray objects => objects.Values.Count > 0,
            EsgFieldValue.Object obj => obj.Value is JsonDocument doc && doc.RootElement.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined && doc.RootElement.GetRawText().Length > 2,
            EsgFieldValue.Boolean => true,
            EsgFieldValue.Number => true,
            EsgFieldValue.Integer => true,
            _ => false
        };
    }
}

/// <summary>
/// Root aggregate for the full ESG input payload consumed by the calculation engine.
/// </summary>
public sealed record EsgInputPayload(
    IReadOnlyDictionary<string, EsgSectionInput> Sections,
    IReadOnlyDictionary<string, string> Metadata)
{
    public EsgSectionInput GetSection(string sectionCode)
    {
        if (!Sections.TryGetValue(sectionCode, out var section))
        {
            throw new KeyNotFoundException($"Section '{sectionCode}' was not present in validated input.");
        }
        return section;
    }

    public EsgSectionInput? TryGetSection(string sectionCode)
        => Sections.TryGetValue(sectionCode, out var section) ? section : null;
}
