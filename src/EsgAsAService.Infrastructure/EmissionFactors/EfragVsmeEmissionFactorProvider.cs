using System.Net.Http.Json;
using EsgAsAService.Application.Abstractions;
using EsgAsAService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EsgAsAService.Infrastructure.EmissionFactors;

public class EfragVsmeOptions
{
    public string? BaseUrl { get; set; } // e.g., https://example.org/efrag/vsme
}

/// <summary>
/// An optional provider that maps disclosure codes from EFRAG VSME digital template to emission factors when available.
/// The real VSME template primarily defines disclosures/metrics; here we allow hosting a mapping JSON.
/// </summary>
public class EfragVsmeEmissionFactorProvider : IEmissionFactorProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<EfragVsmeEmissionFactorProvider> _logger;
    private readonly EfragVsmeOptions _options;

    public EfragVsmeEmissionFactorProvider(HttpClient http, IOptions<EfragVsmeOptions> options, ILogger<EfragVsmeEmissionFactorProvider> logger)
    {
        _http = http;
        _logger = logger;
        _options = options.Value;
    }

    private record FactorDto(string category, string unit, double kgCo2ePerUnit);

    public async Task<EmissionFactor?> GetFactorAsync(string category, string unit, DateOnly? onDate = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            return null;
        }

        try
        {
            var url = $"{_options.BaseUrl!.TrimEnd('/')}/factors?category={Uri.EscapeDataString(category)}&unit={Uri.EscapeDataString(unit)}";
            var dto = await _http.GetFromJsonAsync<FactorDto>(url, cancellationToken: ct);
            if (dto is null) return null;

            return new EmissionFactor
            {
                Source = "EFRAG VSME",
                Category = dto.category,
                Unit = dto.unit,
                KgCO2ePerUnit = dto.kgCo2ePerUnit
            };
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "EFRAG VSME factor fetch failed for {Category}/{Unit}", category, unit);
            return null;
        }
    }
}

