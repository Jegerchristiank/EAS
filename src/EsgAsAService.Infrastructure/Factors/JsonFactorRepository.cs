using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EsgAsAService.Application.Abstractions;
using EsgAsAService.Domain.Factors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EsgAsAService.Infrastructure.Factors;

public sealed class JsonFactorRepository : IFactorRepository
{
    private readonly ILogger<JsonFactorRepository> _logger;
    private readonly FactorRepositoryOptions _options;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public JsonFactorRepository(ILogger<JsonFactorRepository> logger, IOptions<FactorRepositoryOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task<FactorSet> LoadFactorSetAsync(string version, CancellationToken cancellationToken = default)
    {
        var versionFolder = EnsureVersionFolder(version);
        var autoPath = Path.Combine(versionFolder, "auto.json");
        var customPath = Path.Combine(versionFolder, "custom.json");

        var autoFactors = await ReadFactorFileAsync(autoPath, cancellationToken);
        var customFactors = File.Exists(customPath)
            ? await ReadFactorFileAsync(customPath, cancellationToken)
            : new Dictionary<string, EmissionFactorDefinition>(StringComparer.OrdinalIgnoreCase);

        return new FactorSet(version, autoFactors, customFactors);
    }

    public async Task SaveCustomFactorsAsync(string version, IEnumerable<EmissionFactorDefinition> factors, CancellationToken cancellationToken = default)
    {
        var versionFolder = EnsureVersionFolder(version);
        var customPath = Path.Combine(versionFolder, "custom.json");
        var payload = new FactorFileModel
        {
            Version = version,
            Factors = factors.Select(ToModel).ToList()
        };

        await using var stream = File.Create(customPath);
        await JsonSerializer.SerializeAsync(stream, payload, _serializerOptions, cancellationToken);
        _logger.LogInformation("Saved {Count} custom emission factors to {Path}", payload.Factors.Count, customPath);
    }

    private string EnsureVersionFolder(string version)
    {
        var folder = Path.GetFullPath(Path.Combine(_options.RootPath, version));
        Directory.CreateDirectory(folder);
        return folder;
    }

    private async Task<Dictionary<string, EmissionFactorDefinition>> ReadFactorFileAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Emission factor file not found: {path}", path);
        }

        await using var stream = File.OpenRead(path);
        var payload = await JsonSerializer.DeserializeAsync<FactorFileModel>(stream, _serializerOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Emission factor file {path} could not be deserialised.");

        return payload.Factors
            .Select(FromModel)
            .ToDictionary(f => f.Key, StringComparer.OrdinalIgnoreCase);
    }

    private static FactorFileEntry ToModel(EmissionFactorDefinition definition)
        => new()
        {
            Name = definition.Name,
            Unit = definition.Unit,
            Value = definition.Value,
            Source = definition.Source,
            Region = definition.Region,
            Year = definition.Year,
            ValidFrom = definition.ValidFrom,
            ValidTo = definition.ValidTo,
            Methodology = definition.Methodology
        };

    private static EmissionFactorDefinition FromModel(FactorFileEntry entry)
        => new(
            entry.Name ?? string.Empty,
            entry.Unit ?? string.Empty,
            entry.Value,
            entry.Source ?? string.Empty,
            entry.Region ?? string.Empty,
            entry.Year,
            entry.ValidFrom,
            entry.ValidTo,
            entry.Methodology ?? string.Empty);

    private sealed class FactorFileModel
    {
        public string? Version { get; set; }
        public List<FactorFileEntry> Factors { get; set; } = new();
    }

    private sealed class FactorFileEntry
    {
        public string? Name { get; set; }
        public string? Unit { get; set; }
        public decimal Value { get; set; }
        public string? Source { get; set; }
        public string? Region { get; set; }
        public int? Year { get; set; }
        public DateOnly? ValidFrom { get; set; }
        public DateOnly? ValidTo { get; set; }
        public string? Methodology { get; set; }
    }
}
