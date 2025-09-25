using System.Net.Http.Json;
using EsgAsAService.Web.Models;
using Microsoft.AspNetCore.Components;

namespace EsgAsAService.Web.Services;

public sealed class DirectoryService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DirectoryService> _logger;

    public DirectoryService(HttpClient httpClient, ILogger<DirectoryService> logger, NavigationManager navigation)
    {
        _httpClient = httpClient;
        _logger = logger;
        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(navigation.BaseUri);
        }
    }

    public async Task<IReadOnlyList<OrganizationSummary>> GetActiveOrganizationsAsync(CancellationToken cancellationToken = default)
    {
        var url = "/v1/organisations?status=active";
        return await FetchOrFallbackAsync(url, cancellationToken,
            () => Task.FromResult<IReadOnlyList<OrganizationSummary>>(new[]
            {
                new OrganizationSummary(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Nordic Renewables", "Active", "Energy", 4),
                new OrganizationSummary(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Green Manufacturing A/S", "Active", "Manufacturing", 2),
                new OrganizationSummary(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Sustainability Hub", "Active", "Consulting", 6),
            }));
    }

    public async Task<IReadOnlyList<ReportingPeriodSummary>> GetReportingPeriodsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var url = $"/v1/reporting-periods?orgId={organizationId}";
        return await FetchOrFallbackAsync(url, cancellationToken,
            () => Task.FromResult<IReadOnlyList<ReportingPeriodSummary>>(new[]
            {
                new ReportingPeriodSummary(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "2023", "In progress", DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), "Maja Holm", 0.62),
                new ReportingPeriodSummary(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "2022", "Locked", DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)), "Maja Holm", 1),
            }));
    }

    private async Task<IReadOnlyList<T>> FetchOrFallbackAsync<T>(string url, CancellationToken cancellationToken, Func<Task<IReadOnlyList<T>>> fallback)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<IReadOnlyList<T>>(url, cancellationToken);
            if (result is { Count: > 0 })
            {
                return result;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or NotSupportedException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Falling back to sample data for {Url}", url);
        }

        return await fallback().ConfigureAwait(false);
    }
}
