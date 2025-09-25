using System.Net.Http.Json;
using EsgAsAService.Web.Models;
using Microsoft.AspNetCore.Components;

namespace EsgAsAService.Web.Services;

public sealed class DiagnosticsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DiagnosticsClient> _logger;

    public DiagnosticsClient(HttpClient httpClient, ILogger<DiagnosticsClient> logger, NavigationManager navigation)
    {
        _httpClient = httpClient;
        _logger = logger;
        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(navigation.BaseUri);
        }
    }

    public async Task<IReadOnlyList<DiagnosticIssue>> GetUnresolvedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var issues = await _httpClient.GetFromJsonAsync<IReadOnlyList<DiagnosticIssue>>("/v1/diagnostics/unresolved", cancellationToken).ConfigureAwait(false);
            return issues ?? Array.Empty<DiagnosticIssue>();
        }
        catch (Exception ex) when (ex is HttpRequestException or NotSupportedException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Unable to load diagnostics. Showing fallback message.");
            return new[]
            {
                new DiagnosticIssue("UI-FALLBACK", "warning", "Diagnostics API utilgængelig – viser cached status", DateTimeOffset.UtcNow)
            };
        }
    }
}
