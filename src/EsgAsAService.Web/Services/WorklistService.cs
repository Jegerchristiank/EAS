using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;

namespace EsgAsAService.Web.Services;

public sealed class WorklistService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WorklistService> _logger;

    public WorklistService(HttpClient httpClient, ILogger<WorklistService> logger, NavigationManager navigation)
    {
        _httpClient = httpClient;
        _logger = logger;
        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(navigation.BaseUri);
        }
    }

    public async Task<int> GetOpenTaskCountAsync(Guid? organizationId, CancellationToken cancellationToken = default)
    {
        var url = organizationId.HasValue ? $"/v1/tasks?status=open&orgId={organizationId}" : "/v1/tasks?status=open";
        try
        {
            var tasks = await _httpClient.GetFromJsonAsync<List<object>>(url, cancellationToken).ConfigureAwait(false);
            return tasks?.Count ?? 0;
        }
        catch (Exception ex) when (ex is HttpRequestException or NotSupportedException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Unable to fetch open tasks count from {Url}; using placeholder", url);
            return 7;
        }
    }
}
