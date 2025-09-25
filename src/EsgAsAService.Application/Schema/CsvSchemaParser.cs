using System.Globalization;
using System.IO;
using EsgAsAService.Application.Abstractions;
using EsgAsAService.Domain.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;

namespace EsgAsAService.Application.Schema;

/// <summary>
/// Parses the ESG datapoint CSV catalogue into a strongly typed schema representation.
/// </summary>
public sealed class CsvSchemaParser
{
    private readonly ILogger<CsvSchemaParser> _logger;

    private static readonly IReadOnlyDictionary<string, int> ExpectedSectionCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["B1"] = 13,
        ["B2"] = 30,
        ["B3"] = 14,
        ["B4"] = 4,
        ["B5"] = 4,
        ["B6"] = 2,
        ["B7"] = 6,
        ["B8"] = 10,
        ["B9"] = 5,
        ["B10"] = 6,
        ["B11"] = 2,
        ["C1"] = 10,
        ["C2"] = 3,
        ["C3"] = 4,
        ["C4"] = 5,
        ["C5"] = 3,
        ["C6"] = 3,
        ["C7"] = 4,
        ["C8"] = 10,
        ["C9"] = 1
    };

    public CsvSchemaParser(ILogger<CsvSchemaParser> logger)
    {
        _logger = logger;
    }

    public SchemaParseResult Parse(Stream csvStream, string sourcePath)
    {
        ArgumentNullException.ThrowIfNull(csvStream);

        using var parser = new TextFieldParser(csvStream)
        {
            TextFieldType = FieldType.Delimited,
            Delimiters = [","],
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = true
        };

        if (parser.EndOfData)
        {
            throw new InvalidOperationException("Datapoint CSV appears to be empty.");
        }

        // Skip header
        _ = parser.ReadFields();

        var sections = new Dictionary<string, SectionBuilder>(StringComparer.OrdinalIgnoreCase);
        var formulaMap = new Dictionary<string, SchemaFormulaMapping>(StringComparer.OrdinalIgnoreCase);
        var generatedAt = File.Exists(sourcePath)
            ? new DateTimeOffset(File.GetLastWriteTimeUtc(sourcePath))
            : DateTimeOffset.UtcNow;

        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields is null || fields.Length < 11)
            {
                _logger.LogWarning("Skipping malformed CSV line {LineNumber}", parser.LineNumber);
                continue;
            }

            var module = Safe(fields[0]);
            var sectionCode = Safe(fields[1]);
            var sectionName = Safe(fields[2]);
            var datapointKey = Safe(fields[3]);
            var datapointLabel = Safe(fields[4]);
            var datatypeRaw = Safe(fields[5]);
            var unit = ToNull(fields[6]);
            var allowedValuesRaw = Safe(fields[7]);
            var requirementRaw = Safe(fields[8]);
            var dependsOn = ToNull(fields[9]);
            var notes = ToNull(fields[10]);

            if (string.IsNullOrWhiteSpace(sectionCode) || string.IsNullOrWhiteSpace(datapointKey))
            {
                _logger.LogDebug("Skipping row without section/datapoint at line {LineNumber}", parser.LineNumber);
                continue;
            }

            var dataType = ToDataType(datatypeRaw);
            var requirement = ToRequirement(requirementRaw);
            var visibility = string.IsNullOrWhiteSpace(dependsOn) ? null : new FieldVisibilityCondition(dependsOn);
            var allowedValues = ParseAllowedValues(allowedValuesRaw);

            if (!sections.TryGetValue(sectionCode, out var section))
            {
                section = new SectionBuilder(module, sectionCode, sectionName);
                sections[sectionCode] = section;
            }

            var field = new EsgField(
                DatapointKey: datapointKey,
                Label: datapointLabel,
                DataType: dataType,
                Unit: unit,
                AllowedValues: allowedValues,
                Requirement: requirement,
                Visibility: visibility,
                Notes: notes);

            section.Fields.Add(field);

            formulaMap[datapointKey] = new SchemaFormulaMapping(
                SectionCode: sectionCode,
                DatapointKey: datapointKey,
                ModuleCalculation: null,
                FormulaReference: null,
                CoverageStatus: "wizard_input");
        }

        var schema = BuildSchema(sections, sourcePath, generatedAt);
        ValidateCounts(schema);

        return new SchemaParseResult(schema, formulaMap);
    }

    private static EsgInputSchema BuildSchema(Dictionary<string, SectionBuilder> sections, string sourcePath, DateTimeOffset generatedAt)
    {
        var ordered = sections.Values
            .OrderBy(s => s.SectionCode, StringComparer.OrdinalIgnoreCase)
            .Select(builder => new EsgSection(
                builder.Module,
                builder.SectionCode,
                builder.SectionName,
                builder.Fields
                    .OrderBy(f => f.DatapointKey, StringComparer.OrdinalIgnoreCase)
                    .ToArray()))
            .ToArray();

        return new EsgInputSchema(ordered, sourcePath, generatedAt);
    }

    private static void ValidateCounts(EsgInputSchema schema)
    {
        foreach (var expected in ExpectedSectionCounts)
        {
            var section = schema.Sections.FirstOrDefault(s => string.Equals(s.SectionCode, expected.Key, StringComparison.OrdinalIgnoreCase));
            if (section is null)
            {
                throw new InvalidOperationException($"Datapoint CSV missing section {expected.Key} required by spec.");
            }

            if (section.Fields.Count != expected.Value)
            {
                throw new InvalidOperationException($"Section {section.SectionCode} expected {expected.Value} datapoints but found {section.Fields.Count}.");
            }
        }
    }

    private static string Safe(string? value) => value?.Trim() ?? string.Empty;
    private static string? ToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IReadOnlyList<string> ParseAllowedValues(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
        var trimmed = raw.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '[' && trimmed[^1] == ']')
        {
            trimmed = trimmed[1..^1];
        }

        if (string.IsNullOrWhiteSpace(trimmed)) return Array.Empty<string>();

        var split = trimmed
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim().Trim('\'', '\"'))
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return split.Length == 0 ? Array.Empty<string>() : split;
    }

    private static EsgFieldDataType ToDataType(string raw) => raw switch
    {
        "boolean" => EsgFieldDataType.Boolean,
        "string" => EsgFieldDataType.String,
        "string[]" => EsgFieldDataType.StringArray,
        "array<string>" => EsgFieldDataType.StringArray,
        "number" => EsgFieldDataType.Number,
        "integer" => EsgFieldDataType.Integer,
        "object" => EsgFieldDataType.Object,
        "array<object>" => EsgFieldDataType.ObjectArray,
        _ => EsgFieldDataType.StringList
    };

    private static EsgFieldRequirement ToRequirement(string raw) => raw switch
    {
        "skal" => EsgFieldRequirement.Required,
        "kan" => EsgFieldRequirement.Optional,
        "skal_hvis_relevant" => EsgFieldRequirement.Conditional,
        string empty when string.IsNullOrWhiteSpace(empty) => EsgFieldRequirement.Optional,
        _ => throw new InvalidOperationException($"Unknown requirement '{raw}'.")
    };

    private sealed class SectionBuilder(string module, string sectionCode, string sectionName)
    {
        public string Module { get; } = module;
        public string SectionCode { get; } = sectionCode;
        public string SectionName { get; } = sectionName;
        public List<EsgField> Fields { get; } = new();
    }
}

public sealed record SchemaParseResult(EsgInputSchema Schema, IReadOnlyDictionary<string, SchemaFormulaMapping> FormulaMap);
