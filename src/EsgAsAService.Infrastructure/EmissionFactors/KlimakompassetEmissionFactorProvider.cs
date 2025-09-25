using System.Net.Http.Json;
using EsgAsAService.Application.Abstractions;
using EsgAsAService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EsgAsAService.Infrastructure.EmissionFactors;

public class KlimakompassetOptions
{
    public string? BaseUrl { get; set; } // e.g., https://api.klimakompasset.dk (example)
}

/// <summary>
/// Attempts to fetch emission factors from Klimakompasset if configured.
/// If BaseUrl is null or request fails, returns null.
/// </summary>
public class KlimakompassetEmissionFactorProvider : IEmissionFactorProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<KlimakompassetEmissionFactorProvider> _logger;
    private readonly KlimakompassetOptions _options;

    public KlimakompassetEmissionFactorProvider(HttpClient http, IOptions<KlimakompassetOptions> options, ILogger<KlimakompassetEmissionFactorProvider> logger)
    {
        _http = http;
        _logger = logger;
        _options = options.Value;
    }

    private record FactorDto(string category, string unit, double kgCo2ePerUnit, string? validFrom, string? validTo);

    public async Task<EmissionFactor?> GetFactorAsync(string category, string unit, DateOnly? onDate = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            return null;
        }

        try
        {
            // Example path; adjust to the real API when available
            var url = $"{_options.BaseUrl!.TrimEnd('/')}/emission-factors?category={Uri.EscapeDataString(category)}&unit={Uri.EscapeDataString(unit)}";
            var dto = await _http.GetFromJsonAsync<FactorDto>(url, cancellationToken: ct);
            if (dto is null) return null;

            return new EmissionFactor
            {
                Source = "Klimakompasset",
                Category = dto.category,
                Unit = dto.unit,
                KgCO2ePerUnit = dto.kgCo2ePerUnit,
                ValidFrom = DateOnly.TryParse(dto.validFrom, out var vf) ? vf : null,
                ValidTo = DateOnly.TryParse(dto.validTo, out var vt) ? vt : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Klimakompasset factor fetch failed for {Category}/{Unit}", category, unit);
            return null;
        }
    }
}

