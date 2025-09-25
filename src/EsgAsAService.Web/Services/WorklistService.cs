using System.Net.Http.Json;
using EsgAsAService.Web.Models;
using Microsoft.AspNetCore.Components;
using System.Linq;

namespace EsgAsAService.Web.Services;

public sealed class WorklistService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WorklistService> _logger;
    private static readonly IReadOnlyList<TaskSummary> _sampleTasks = new[]
    {
        new TaskSummary(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Godkend energidata", "Energi", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)), "Jonas", "open", "/tasks/energy"),
        new TaskSummary(Guid.Parse("55555555-5555-5555-5555-555555555555"), "Upload HR-data", "HR", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), "Liva", "open", "/tasks/hr"),
        new TaskSummary(Guid.Parse("66666666-6666-6666-6666-666666666666"), "Afklar afvigelse", "Governance", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), "Noah", "overdue", "/tasks/issue"),
        new TaskSummary(Guid.Parse("77777777-7777-7777-7777-777777777777"), "Uploade vandforbrug", "Vand", DateOnly.FromDateTime(DateTime.UtcNow), "Maja Holm", "overdue", "/tasks/water"),
        new TaskSummary(Guid.Parse("88888888-8888-8888-8888-888888888888"), "Lås rapportperiode", "Rapporter", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), "Maja Holm", "open", "/tasks/period"),
        new TaskSummary(Guid.Parse("99999999-9999-9999-9999-999999999999"), "Arkivér afsluttet rapport", "Rapporter", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14)), "Jonas", "completed", "/tasks/archive"),
    };

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

    public async Task<IReadOnlyList<TaskSummary>> GetTasksAsync(TaskQuery query, CancellationToken cancellationToken = default)
    {
        var parameters = new List<string>();
        if (query.OrganizationId.HasValue)
        {
            parameters.Add($"orgId={query.OrganizationId}");
        }

        if (query.PeriodId.HasValue)
        {
            parameters.Add($"periodId={query.PeriodId}");
        }

        if (!string.IsNullOrWhiteSpace(query.Status) && !string.Equals(query.Status, "all", StringComparison.OrdinalIgnoreCase))
        {
            parameters.Add($"status={query.Status}");
        }

        if (!string.IsNullOrWhiteSpace(query.Scope) && !string.Equals(query.Scope, "all", StringComparison.OrdinalIgnoreCase))
        {
            parameters.Add($"scope={query.Scope}");
        }

        var url = "/v1/tasks";
        if (parameters.Count > 0)
        {
            url = string.Concat(url, "?", string.Join('&', parameters));
        }

        try
        {
            var tasks = await _httpClient.GetFromJsonAsync<IReadOnlyList<TaskSummary>>(url, cancellationToken).ConfigureAwait(false);
            if (tasks is { Count: > 0 })
            {
                return tasks;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or NotSupportedException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Unable to fetch task worklist from {Url}; using sample data", url);
        }

        return ApplyFallbackFilters(query);
    }

    private static IReadOnlyList<TaskSummary> ApplyFallbackFilters(TaskQuery query)
    {
        IEnumerable<TaskSummary> filtered = _sampleTasks;

        if (!string.IsNullOrWhiteSpace(query.Status) && !string.Equals(query.Status, "all", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(t => string.Equals(t.Status, query.Status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Scope) && !string.Equals(query.Scope, "all", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(query.Scope, "mine", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(query.Owner))
            {
                filtered = filtered.Where(t => string.Equals(t.Assignee, query.Owner, StringComparison.OrdinalIgnoreCase));
            }
            else if (string.Equals(query.Scope, "team", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(query.Owner))
            {
                filtered = filtered.Where(t => !string.Equals(t.Assignee, query.Owner, StringComparison.OrdinalIgnoreCase));
            }
        }

        return filtered
            .OrderBy(t => t.DueDate ?? DateOnly.MaxValue)
            .ToList();
    }
}
