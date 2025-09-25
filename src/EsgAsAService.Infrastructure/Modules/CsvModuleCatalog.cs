using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EsgAsAService.Application.Abstractions;
using EsgAsAService.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;

namespace EsgAsAService.Infrastructure.Modules;

public class CsvModuleCatalog : IModuleCatalog
{
    private readonly ILogger<CsvModuleCatalog> _logger;
    private readonly Lazy<Task<IReadOnlyList<EsgModuleDefinition>>> _lazyModules;

    private static readonly string[] Delimiters = [","];

    public CsvModuleCatalog(ILogger<CsvModuleCatalog> logger)
    {
        _logger = logger;
        _lazyModules = new Lazy<Task<IReadOnlyList<EsgModuleDefinition>>>(LoadAsync, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public Task<IReadOnlyList<EsgModuleDefinition>> GetModulesAsync(CancellationToken ct = default)
        => _lazyModules.Value;

    public async Task<EsgModuleDefinition?> GetModuleAsync(string sectionCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sectionCode)) return null;
        var modules = await GetModulesAsync(ct);
        return modules.FirstOrDefault(m => string.Equals(m.SectionCode, sectionCode, StringComparison.OrdinalIgnoreCase));
    }

    private Task<IReadOnlyList<EsgModuleDefinition>> LoadAsync()
    {
        try
        {
            var path = ResolveCatalogPath();
            if (!File.Exists(path))
            {
                _logger.LogWarning("Module catalog file not found at {Path}", path);
                return Task.FromResult<IReadOnlyList<EsgModuleDefinition>>(Array.Empty<EsgModuleDefinition>());
            }

            using var parser = new TextFieldParser(path)
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true,
                Delimiters = Delimiters,
                TrimWhiteSpace = true
            };

            // Skip header
            if (!parser.EndOfData)
            {
                _ = parser.ReadFields();
            }

            var sections = new Dictionary<string, EsgModuleDefinition>(StringComparer.OrdinalIgnoreCase);
            while (!parser.EndOfData)
            {
                var fields = parser.ReadFields();
                if (fields is null || fields.Length == 0) continue;
                if (fields.Length < 11)
                {
                    _logger.LogDebug("Catalog line {Line} does not contain expected columns (found {Count})", parser.LineNumber, fields.Length);
                    continue;
                }

                var moduleName = Safe(fields[0]);
                var sectionCode = Safe(fields[1]);
                var sectionName = Safe(fields[2]);
                var datapointKey = Safe(fields[3]);
                var datapointLabel = Safe(fields[4]);
                var datatype = Safe(fields[5]);
                var unit = Safe(fields[6]);
                var allowedValuesRaw = Safe(fields[7]);
                var requirement = Safe(fields[8]);
                var dependsOn = Safe(fields[9]);
                var notes = Safe(fields[10]);

                if (string.IsNullOrWhiteSpace(sectionCode) || string.IsNullOrWhiteSpace(datapointKey))
                {
                    continue;
                }

                if (!sections.TryGetValue(sectionCode, out var section))
                {
                    section = new EsgModuleDefinition
                    {
                        Module = moduleName,
                        SectionCode = sectionCode,
                        SectionName = sectionName
                    };
                    sections[sectionCode] = section;
                }

                section.Datapoints.Add(new EsgModuleDatapoint
                {
                    Key = datapointKey,
                    Label = datapointLabel,
                    DataType = ToNull(datatype),
                    Unit = ToNull(unit),
                    Requirement = ToNull(requirement),
                    DependsOn = ToNull(dependsOn),
                    Notes = ToNull(notes),
                    AllowedValues = ParseAllowedValues(allowedValuesRaw)
                });
            }

            var ordered = sections.Values
                .OrderBy(s => s.SectionCode, StringComparer.OrdinalIgnoreCase)
                .ThenBy(s => s.SectionName, StringComparer.OrdinalIgnoreCase)
                .Select(section =>
                {
                    section.Datapoints = section.Datapoints
                        .OrderBy(d => d.Key, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    return section;
                })
                .ToList();

            _logger.LogInformation("Loaded {SectionCount} sections with {DatapointCount} datapoints from module catalog", ordered.Count, ordered.Sum(x => x.Datapoints.Count));
            return Task.FromResult<IReadOnlyList<EsgModuleDefinition>>(ordered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load module catalog");
            return Task.FromResult<IReadOnlyList<EsgModuleDefinition>>(Array.Empty<EsgModuleDefinition>());
        }
    }

    private static string ResolveCatalogPath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var primary = Path.Combine(baseDirectory, "Resources", "ESGModules.csv");
        if (File.Exists(primary)) return primary;

        // Fallback when running from repository root without build output
        var fallback = Path.Combine(baseDirectory, "..", "..", "docs", "ESG_datapunkter__B1-B11__C1-C9_.csv");
        return Path.GetFullPath(fallback);
    }

    private static string Safe(string? value) => value?.Trim() ?? string.Empty;
    private static string? ToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IReadOnlyList<string> ParseAllowedValues(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
        var trimmed = raw.Trim();
        if (trimmed.Length >= 2 && trimmed[0] is '[' && trimmed[^1] is ']')
        {
            trimmed = trimmed[1..^1];
        }
        if (string.IsNullOrWhiteSpace(trimmed)) return Array.Empty<string>();

        var values = trimmed
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim().Trim('\'', '\"'))
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return values.Length == 0 ? Array.Empty<string>() : values;
    }
}
