using EsgAsAService.Application.Abstractions;
using EsgAsAService.Application.Schema;
using EsgAsAService.Infrastructure.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<EsgSchemaOptions>(options =>
{
    options.CsvPath = builder.Configuration["EsgSchema:CsvPath"]
        ?? builder.Configuration["csv"]
        ?? Environment.GetEnvironmentVariable("ESG_CSV_PATH");
});

builder.Services.AddLogging(logging =>
{
    logging.AddSimpleConsole(o =>
    {
        o.SingleLine = true;
        o.TimestampFormat = "HH:mm:ss ";
    });
});

builder.Services.AddSingleton<CsvSchemaParser>();
builder.Services.AddSingleton<IEsgInputSchemaProvider, FileSystemEsgInputSchemaProvider>();
builder.Services.AddSingleton<SchemaCoverageRunner>();

using var host = builder.Build();

var runner = host.Services.GetRequiredService<SchemaCoverageRunner>();
var result = await runner.RunAsync();

return result ? 0 : 1;

internal sealed class SchemaCoverageRunner(
    IEsgInputSchemaProvider provider,
    ILogger<SchemaCoverageRunner> logger,
    IHostEnvironment env,
    IOptions<EsgSchemaOptions> options)
{
    public async Task<bool> RunAsync()
    {
        logger.LogInformation("Running schema coverage check (environment: {Environment})", env.EnvironmentName);

        var schema = await provider.GetSchemaAsync();
        var formulaMap = await provider.GetFormulaMapAsync();

        var totalFields = schema.Sections.Sum(s => s.Fields.Count);
        logger.LogInformation("Schema contains {Sections} sections and {Fields} datapoints", schema.Sections.Count, totalFields);

        var invalidCoverage = formulaMap.Values
            .Where(m => !AllowedCoverageStatuses.Contains(m.CoverageStatus))
            .ToArray();

        if (invalidCoverage.Length > 0)
        {
            foreach (var item in invalidCoverage)
            {
                logger.LogError("Invalid coverage status {Status} for datapoint {Datapoint} ({Section})", item.CoverageStatus, item.DatapointKey, item.SectionCode);
            }
            logger.LogError("Schema coverage failed: found {Count} invalid entries", invalidCoverage.Length);
            return false;
        }

        var schemaPath = Path.GetFullPath(options.Value.SchemaOutputPath);
        var formulaPath = Path.GetFullPath(options.Value.FormulaMapOutputPath);

        if (!File.Exists(schemaPath) || !File.Exists(formulaPath))
        {
            logger.LogError("Schema artifacts missing. Expected {SchemaPath} and {FormulaPath}", schemaPath, formulaPath);
            return false;
        }

        logger.LogInformation("Schema artifacts ready: {SchemaPath}, {FormulaPath}", schemaPath, formulaPath);
        logger.LogInformation("Schema coverage check succeeded.");
        return true;
    }

    private static readonly HashSet<string> AllowedCoverageStatuses =
    [
        "wizard_input",
        "derived",
        "not_applicable"
    ];
}
