using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using EsgAsAService.Application.Abstractions;
using EsgAsAService.Application.Schema;
using EsgAsAService.Domain.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EsgAsAService.Infrastructure.Schema;

public sealed class FileSystemEsgInputSchemaProvider : IEsgInputSchemaProvider
{
    private readonly CsvSchemaParser _parser;
    private readonly ILogger<FileSystemEsgInputSchemaProvider> _logger;
    private readonly EsgSchemaOptions _options;
    private readonly Lazy<Task<Cache>> _lazy;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public FileSystemEsgInputSchemaProvider(
        CsvSchemaParser parser,
        IOptions<EsgSchemaOptions> options,
        ILogger<FileSystemEsgInputSchemaProvider> logger)
    {
        _parser = parser;
        _logger = logger;
        _options = options.Value;
        _lazy = new Lazy<Task<Cache>>(LoadAsync, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public async ValueTask<EsgInputSchema> GetSchemaAsync(CancellationToken cancellationToken = default)
    {
        var cache = await _lazy.Value.ConfigureAwait(false);
        return cache.Schema;
    }

    public async ValueTask<IReadOnlyDictionary<string, SchemaFormulaMapping>> GetFormulaMapAsync(CancellationToken cancellationToken = default)
    {
        var cache = await _lazy.Value.ConfigureAwait(false);
        return cache.FormulaMap;
    }

    private async Task<Cache> LoadAsync()
    {
        var csvPath = ResolveCsvPath();
        _logger.LogInformation("Loading ESG input schema from {CsvPath}", csvPath);

        await using var stream = File.OpenRead(csvPath);
        var parseResult = _parser.Parse(stream, csvPath);

        var schema = parseResult.Schema;
        var formulaMap = new Dictionary<string, SchemaFormulaMapping>(parseResult.FormulaMap, StringComparer.OrdinalIgnoreCase);

        ApplyCoverageOverrides(formulaMap);

        await PersistArtifactsAsync(schema, formulaMap, csvPath).ConfigureAwait(false);

        _logger.LogInformation("Loaded schema with {SectionCount} sections and {FieldCount} datapoints", schema.Sections.Count, schema.Sections.Sum(s => s.Fields.Count));

        return new Cache(schema, formulaMap);
    }

    private string ResolveCsvPath()
    {
        if (!string.IsNullOrWhiteSpace(_options.CsvPath))
        {
            var explicitPath = Path.GetFullPath(_options.CsvPath);
            if (!File.Exists(explicitPath))
            {
                throw new FileNotFoundException($"Configured ESG datapoint CSV not found at '{explicitPath}'.", explicitPath);
            }
            return explicitPath;
        }

        var baseDirectory = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDirectory);
        while (dir is not null)
        {
            var potential = Path.Combine(dir.FullName, "docs", "ESG_datapunkter__B1-B11__C1-C9_.csv");
            if (File.Exists(potential))
            {
                return potential;
            }
            dir = dir.Parent;
        }

        var resourceCandidate = Path.Combine(baseDirectory, "Resources", "ESGModules.csv");
        if (File.Exists(resourceCandidate))
        {
            return resourceCandidate;
        }

        throw new FileNotFoundException("Could not locate ESG datapoint CSV. Set EsgSchemaOptions:CsvPath explicitly or ensure docs file exists.");
    }

    private async Task PersistArtifactsAsync(EsgInputSchema schema, IReadOnlyDictionary<string, SchemaFormulaMapping> formulaMap, string csvPath)
    {
        var schemaJson = BuildJsonSchema(schema);
        var formulaJson = BuildFormulaMap(formulaMap.Values);

        var schemaPath = Path.GetFullPath(_options.SchemaOutputPath);
        var formulaPath = Path.GetFullPath(_options.FormulaMapOutputPath);

        Directory.CreateDirectory(Path.GetDirectoryName(schemaPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(formulaPath)!);

        await File.WriteAllTextAsync(schemaPath, schemaJson, cancellationToken: CancellationToken.None).ConfigureAwait(false);
        await File.WriteAllTextAsync(formulaPath, formulaJson, cancellationToken: CancellationToken.None).ConfigureAwait(false);

        _logger.LogInformation("Persisted schema artifact to {SchemaPath} (source {CsvSource})", schemaPath, csvPath);
        _logger.LogInformation("Persisted formula map to {FormulaPath}", formulaPath);
    }

    private static string BuildJsonSchema(EsgInputSchema schema)
    {
        var root = new JsonObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["title"] = "ESG Wizard Input",
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["x-generatedAtUtc"] = schema.GeneratedAtUtc.ToString("O"),
            ["x-source"] = schema.Source
        };

        var properties = new JsonObject();

        foreach (var section in schema.Sections)
        {
            var sectionObject = new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["x-module"] = section.Module,
                ["x-sectionName"] = section.SectionName
            };

            var fieldProperties = new JsonObject();
            var required = new JsonArray();

            foreach (var field in section.Fields)
            {
                var fieldObject = CreateFieldSchema(field);
                fieldProperties[field.DatapointKey] = fieldObject;
                if (field.Requirement == EsgFieldRequirement.Required)
                {
                    required.Add(field.DatapointKey);
                }
            }

            sectionObject["properties"] = fieldProperties;
            if (required.Count > 0)
            {
                sectionObject["required"] = required;
            }

            properties[section.SectionCode] = sectionObject;
        }

        root["properties"] = properties;

        return root.ToJsonString(SerializerOptions);
    }

    private static JsonObject CreateFieldSchema(EsgField field)
    {
        var fieldObject = new JsonObject
        {
            ["title"] = field.Label
        };

        switch (field.DataType)
        {
            case EsgFieldDataType.Boolean:
                fieldObject["type"] = "string";
                if (field.AllowedValues.Count == 0)
                {
                    fieldObject["enum"] = new JsonArray("JA", "NEJ");
                }
                else
                {
                    fieldObject["enum"] = new JsonArray(field.AllowedValues.Select(v => JsonValue.Create(v)).ToArray());
                }
                break;
            case EsgFieldDataType.String:
                fieldObject["type"] = "string";
                break;
            case EsgFieldDataType.StringArray:
                fieldObject["type"] = "array";
                fieldObject["items"] = new JsonObject { ["type"] = "string" };
                break;
            case EsgFieldDataType.Number:
                fieldObject["type"] = "number";
                break;
            case EsgFieldDataType.Integer:
                fieldObject["type"] = "integer";
                break;
            case EsgFieldDataType.Object:
                fieldObject["type"] = "object";
                break;
            case EsgFieldDataType.ObjectArray:
                fieldObject["type"] = "array";
                fieldObject["items"] = new JsonObject { ["type"] = "object" };
                break;
            case EsgFieldDataType.StringList:
                fieldObject["type"] = "string";
                break;
            default:
                fieldObject["type"] = "string";
                break;
        }

        if (field.AllowedValues.Count > 0 && field.DataType != EsgFieldDataType.Boolean)
        {
            fieldObject["enum"] = new JsonArray(field.AllowedValues.Select(v => JsonValue.Create(v)).ToArray());
        }

        if (!string.IsNullOrWhiteSpace(field.Unit))
        {
            fieldObject["x-unit"] = field.Unit;
        }
        if (!string.IsNullOrWhiteSpace(field.Notes))
        {
            fieldObject["description"] = field.Notes;
        }
        if (field.Requirement == EsgFieldRequirement.Conditional)
        {
            fieldObject["x-requirement"] = "conditional";
        }
        else if (field.Requirement == EsgFieldRequirement.Optional)
        {
            fieldObject["x-requirement"] = "optional";
        }

        if (field.Visibility is not null)
        {
            fieldObject["x-dependsOn"] = field.Visibility.Expression;
        }

        return fieldObject;
    }

    private static string BuildFormulaMap(IEnumerable<SchemaFormulaMapping> mappings)
    {
        var array = new JsonArray();
        foreach (var mapping in mappings.OrderBy(m => m.SectionCode, StringComparer.OrdinalIgnoreCase).ThenBy(m => m.DatapointKey, StringComparer.OrdinalIgnoreCase))
        {
            var obj = new JsonObject
            {
                ["section"] = mapping.SectionCode,
                ["datapoint"] = mapping.DatapointKey,
                ["module_calculation"] = mapping.ModuleCalculation is null ? null : JsonValue.Create(mapping.ModuleCalculation),
                ["formula_reference"] = mapping.FormulaReference is null ? null : JsonValue.Create(mapping.FormulaReference),
                ["coverage_status"] = mapping.CoverageStatus
            };
            array.Add(obj);
        }

        return array.ToJsonString(SerializerOptions);
    }

    private sealed record Cache(EsgInputSchema Schema, IReadOnlyDictionary<string, SchemaFormulaMapping> FormulaMap);

    private static void ApplyCoverageOverrides(IDictionary<string, SchemaFormulaMapping> formulaMap)
    {
        foreach (var (datapointKey, descriptor) in CoverageOverrides)
        {
            if (formulaMap.TryGetValue(datapointKey, out var mapping))
            {
                formulaMap[datapointKey] = mapping with
                {
                    ModuleCalculation = descriptor.ModuleCalculation,
                    FormulaReference = descriptor.FormulaReference,
                    CoverageStatus = descriptor.CoverageStatus
                };
            }
        }
    }

    private sealed record CoverageOverride(string ModuleCalculation, string FormulaReference, string CoverageStatus);

    private static readonly IReadOnlyDictionary<string, CoverageOverride> CoverageOverrides = new Dictionary<string, CoverageOverride>(StringComparer.OrdinalIgnoreCase)
    {
        // B-series coverage
        ["antal_ansatte"] = new("RunB1", "docs/esg-formulas.md#b1--company--basis", "derived"),
        ["balancesum_eur"] = new("RunB1", "docs/esg-formulas.md#b1--company--basis", "derived"),
        ["omsaetning_eur"] = new("RunB1", "docs/esg-formulas.md#b1--company--basis", "derived"),
        ["sites"] = new("RunB1", "docs/esg-formulas.md#b1--company--basis", "derived"),
        ["certifikater_miljoemaerker"] = new("RunB1", "docs/esg-formulas.md#b1--company--basis", "derived"),
        ["klima_har_politik"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["klima_offentlig"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["klima_har_maal_initiativer"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["forurening_har_politik"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["forurening_offentlig"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["forurening_har_maal_initiativer"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["vand_hav_har_politik"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["vand_hav_offentlig"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["vand_hav_har_maal_initiativer"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["biodiversitet_har_politik"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["biodiversitet_offentlig"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["biodiversitet_har_maal_initiativer"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["cirkoekonomi_har_politik"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["cirkoekonomi_offentlig"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["cirkoekonomi_har_maal_initiativer"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["egen_arbejdsstyrke_har_politik"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["egen_arbejdsstyrke_offentlig"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["egen_arbejdsstyrke_har_maal_initiativer"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["vaerdikaede_arbejdere_har_politik"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["vaerdikaede_arbejdere_offentlig"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["vaerdikaede_arbejdere_har_maal_initiativer"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["beroerte_samfund_har_politik"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["beroerte_samfund_offentlig"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["beroerte_samfund_har_maal_initiativer"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["forbrugere_har_politik"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["forbrugere_offentlig"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["forbrugere_har_maal_initiativer"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["virksomhedsledelse_har_politik"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["virksomhedsledelse_offentlig"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["virksomhedsledelse_har_maal_initiativer"] = new("RunB2", "docs/esg-formulas.md#b2--policies", "derived"),
        ["scope1_ton"] = new("RunB3", "docs/esg-formulas.md#b3--energy--emissions", "derived"),
        ["scope2_ton"] = new("RunB3", "docs/esg-formulas.md#b3--energy--emissions", "derived"),
        ["scope3_ton"] = new("RunB3", "docs/esg-formulas.md#b3--energy--emissions", "derived"),
        ["el_ikke_vedvarende_mwh"] = new("RunB3", "docs/esg-formulas.md#b3--energy--emissions", "derived"),
        ["el_vedvarende_mwh"] = new("RunB3", "docs/esg-formulas.md#b3--energy--emissions", "derived"),
        ["braendstof_ikke_vedvarende_mwh"] = new("RunB3", "docs/esg-formulas.md#b3--energy--emissions", "derived"),
        ["braendstof_vedvarende_mwh"] = new("RunB3", "docs/esg-formulas.md#b3--energy--emissions", "derived"),
        ["anden_energi_mwh"] = new("RunB3", "docs/esg-formulas.md#b3--energy--emissions", "derived"),
        ["forurening_maengde"] = new("RunB4", "docs/esg-formulas.md#b4--pollution", "derived"),
        ["forurening_type"] = new("RunB4", "docs/esg-formulas.md#b4--pollution", "derived"),
        ["forurening_medium"] = new("RunB4", "docs/esg-formulas.md#b4--pollution", "derived"),
        ["arealforbrug_seneste_aar_hektar"] = new("RunB5", "docs/esg-formulas.md#b5--biodiversity", "derived"),
        ["arealforbrug_rapportaar_hektar"] = new("RunB5", "docs/esg-formulas.md#b5--biodiversity", "derived"),
        ["arealforbrug_aendring_pct"] = new("RunB5", "docs/esg-formulas.md#b5--biodiversity", "derived"),
        ["bio_følsomme_omraader"] = new("RunB5", "docs/esg-formulas.md#b5--biodiversity", "derived"),
        ["vand_forbrug_m3"] = new("RunB6", "docs/esg-formulas.md#b6--water", "derived"),
        ["vand_udtagning_m3"] = new("RunB6", "docs/esg-formulas.md#b6--water", "derived"),
        ["affald_total_farligt_ton"] = new("RunB7", "docs/esg-formulas.md#b7--resources--waste", "derived"),
        ["affald_total_ikke_farligt_ton"] = new("RunB7", "docs/esg-formulas.md#b7--resources--waste", "derived"),
        ["affald_til_genbrug_genanvendelse_ton"] = new("RunB7", "docs/esg-formulas.md#b7--resources--waste", "derived"),
        ["cirkulaer_ja"] = new("RunB7", "docs/esg-formulas.md#b7--resources--waste", "derived"),
        ["medarbejderomsaetning_pct"] = new("RunB8", "docs/esg-formulas.md#b8--workforce", "derived"),
        ["kontrakttype_midlt"] = new("RunB8", "docs/esg-formulas.md#b8--workforce", "derived"),
        ["kontrakttype_total"] = new("RunB8", "docs/esg-formulas.md#b8--workforce", "derived"),
        ["koen_maend"] = new("RunB8", "docs/esg-formulas.md#b8--workforce", "derived"),
        ["koen_kvinder"] = new("RunB8", "docs/esg-formulas.md#b8--workforce", "derived"),
        ["koen_andet"] = new("RunB8", "docs/esg-formulas.md#b8--workforce", "derived"),
        ["koen_ikke_reg"] = new("RunB8", "docs/esg-formulas.md#b8--workforce", "derived"),
        ["ulykker_frekvens"] = new("RunB9", "docs/esg-formulas.md#b9--health--safety", "derived"),
        ["ulykker_antal"] = new("RunB9", "docs/esg-formulas.md#b9--health--safety", "derived"),
        ["doedsfald_skade"] = new("RunB9", "docs/esg-formulas.md#b9--health--safety", "derived"),
        ["doedsfald_helbred"] = new("RunB9", "docs/esg-formulas.md#b9--health--safety", "derived"),
        ["loenforskel_maend_kvinder_pct"] = new("RunB10", "docs/esg-formulas.md#b10--pay--training", "derived"),
        ["overenskomst_daeckning_pct"] = new("RunB10", "docs/esg-formulas.md#b10--pay--training", "derived"),
        ["uddannelsestimer_maend"] = new("RunB10", "docs/esg-formulas.md#b10--pay--training", "derived"),
        ["uddannelsestimer_kvinder"] = new("RunB10", "docs/esg-formulas.md#b10--pay--training", "derived"),
        ["uddannelsestimer_andre"] = new("RunB10", "docs/esg-formulas.md#b10--pay--training", "derived"),
        ["boeder_samlet_kroner"] = new("RunB11", "docs/esg-formulas.md#b11--business-conduct", "derived"),
        ["domme_antal"] = new("RunB11", "docs/esg-formulas.md#b11--business-conduct", "derived"),

        // C-series coverage
        ["produkter_tjenester_beskrivelse"] = new("RunC1", "docs/esg-formulas.md#c1--strategy--targets", "derived"),
        ["markeder_beskrivelse"] = new("RunC1", "docs/esg-formulas.md#c1--strategy--targets", "derived"),
        ["forretningsforbindelser_beskrivelse"] = new("RunC1", "docs/esg-formulas.md#c1--strategy--targets", "derived"),
        ["hoejeste_ansvarlige_niveau"] = new("RunC1", "docs/esg-formulas.md#c1--strategy--targets", "derived"),
        ["ledelsesorgan_rolle"] = new("RunC1", "docs/esg-formulas.md#c1--strategy--targets", "derived"),
        ["incitamenter_beskrivelse"] = new("RunC1", "docs/esg-formulas.md#c1--strategy--targets", "derived"),
        ["risikostyring_beskrivelse"] = new("RunC1", "docs/esg-formulas.md#c1--strategy--targets", "derived"),
        ["due_diligence_beskrivelse"] = new("RunC1", "docs/esg-formulas.md#c1--strategy--targets", "derived"),
        ["strategiske_maal"] = new("RunC1", "docs/esg-formulas.md#c1--strategy--targets", "derived"),
        ["bæredygtighedsorientering_vurdering"] = new("RunC1", "docs/esg-formulas.md#c1--strategy--targets", "derived"),
        ["indsatser_og_politikker_beskrivelse"] = new("RunC2", "docs/esg-formulas.md#c2--risks", "derived"),
        ["offentlig_tilgængelighed_beskrivelse"] = new("RunC2", "docs/esg-formulas.md#c2--risks", "derived"),
        ["ansvarsniveau_beskrivelse"] = new("RunC2", "docs/esg-formulas.md#c2--risks", "derived"),
        ["reduktionsmaal"] = new("RunC3", "docs/esg-formulas.md#c3--human-rights", "derived"),
        ["baseline_aar"] = new("RunC3", "docs/esg-formulas.md#c3--human-rights", "derived"),
        ["handlinger_liste"] = new("RunC3", "docs/esg-formulas.md#c3--human-rights", "derived"),
        ["omstillingsplan_beskrivelse"] = new("RunC3", "docs/esg-formulas.md#c3--human-rights", "derived"),
        ["klimarisici_beskrivelse"] = new("RunC4", "docs/esg-formulas.md#c4--governance", "derived"),
        ["eksponering_sårbarhed"] = new("RunC4", "docs/esg-formulas.md#c4--governance", "derived"),
        ["tidshorisont"] = new("RunC4", "docs/esg-formulas.md#c4--governance", "derived"),
        ["tilpasning_ja"] = new("RunC4", "docs/esg-formulas.md#c4--governance", "derived"),
        ["finansiel_påvirkning_beskrivelse"] = new("RunC4", "docs/esg-formulas.md#c4--governance", "derived"),
        ["forhold_kvinder_maend_ledelsesniveau"] = new("RunC5", "docs/esg-formulas.md#c5--board-diversity", "derived"),
        ["selvstaendige_antal"] = new("RunC5", "docs/esg-formulas.md#c5--board-diversity", "derived"),
        ["vikarer_antal"] = new("RunC5", "docs/esg-formulas.md#c5--board-diversity", "derived"),
        ["code_of_conduct_eller_mr_politik"] = new("RunC6", "docs/esg-formulas.md#c6--stakeholders", "derived"),
        ["klagemekanisme_ansatte"] = new("RunC6", "docs/esg-formulas.md#c6--stakeholders", "derived"),
        ["mr_processer_beskrivelse"] = new("RunC6", "docs/esg-formulas.md#c6--stakeholders", "derived"),
        ["haendelser_egen_arbejdsstyrke"] = new("RunC7", "docs/esg-formulas.md#c7--value-chain", "derived"),
        ["handlinger_egen_arbejdsstyrke"] = new("RunC7", "docs/esg-formulas.md#c7--value-chain", "derived"),
        ["haendelser_vaerdikaede"] = new("RunC7", "docs/esg-formulas.md#c7--value-chain", "derived"),
        ["handlinger_vaerdikaede"] = new("RunC7", "docs/esg-formulas.md#c7--value-chain", "derived"),
        ["indtægt_kontroversielle_vaaben_dkk"] = new("RunC8", "docs/esg-formulas.md#c8--assurance", "derived"),
        ["indtægt_tobak_dkk"] = new("RunC8", "docs/esg-formulas.md#c8--assurance", "derived"),
        ["indtægt_kul_dkk"] = new("RunC8", "docs/esg-formulas.md#c8--assurance", "derived"),
        ["indtægt_olie_dkk"] = new("RunC8", "docs/esg-formulas.md#c8--assurance", "derived"),
        ["indtægt_gas_dkk"] = new("RunC8", "docs/esg-formulas.md#c8--assurance", "derived"),
        ["indtægt_pesticid_kemi_dkk"] = new("RunC8", "docs/esg-formulas.md#c8--assurance", "derived"),
        ["eu_benchmark_kul_over1pct"] = new("RunC8", "docs/esg-formulas.md#c8--assurance", "derived"),
        ["eu_benchmark_olie_over10pct"] = new("RunC8", "docs/esg-formulas.md#c8--assurance", "derived"),
        ["eu_benchmark_gas_over50pct"] = new("RunC8", "docs/esg-formulas.md#c8--assurance", "derived"),
        ["eu_benchmark_el_100g_over50pct"] = new("RunC8", "docs/esg-formulas.md#c8--assurance", "derived"),
        ["koens_forhold_best"] = new("RunC9", "docs/esg-formulas.md#c9--methodology", "derived")
    };
}
