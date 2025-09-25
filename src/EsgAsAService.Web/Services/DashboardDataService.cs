using System.Net.Http.Json;
using EsgAsAService.Web.Models;
using Microsoft.AspNetCore.Components;

namespace EsgAsAService.Web.Services;

public sealed class DashboardDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DashboardDataService> _logger;

    public DashboardDataService(HttpClient httpClient, ILogger<DashboardDataService> logger, NavigationManager navigation)
    {
        _httpClient = httpClient;
        _logger = logger;
        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(navigation.BaseUri);
        }
    }

    public async Task<DashboardOverview> GetOverviewAsync(Guid? organizationId, Guid? periodId, CancellationToken cancellationToken = default)
    {
        var orgFilter = organizationId.HasValue ? $"&orgId={organizationId}" : string.Empty;
        var tasksUrl = $"/v1/tasks?status=open{orgFilter}";

        var organizationsTask = _httpClient.GetFromJsonAsync<IReadOnlyList<OrganizationSummary>>("/v1/organisations?status=active", cancellationToken);
        var periodsTask = organizationId.HasValue
            ? _httpClient.GetFromJsonAsync<IReadOnlyList<ReportingPeriodSummary>>($"/v1/reporting-periods?orgId={organizationId}", cancellationToken)
            : Task.FromResult<IReadOnlyList<ReportingPeriodSummary>>(Array.Empty<ReportingPeriodSummary>());
        var activitiesTask = _httpClient.GetFromJsonAsync<IReadOnlyList<ActivitySummary>>("/v1/activities", cancellationToken);
        var tasksTask = _httpClient.GetFromJsonAsync<IReadOnlyList<TaskSummary>>(tasksUrl, cancellationToken);

        var organizations = await SafeResultAsync(organizationsTask, SampleOrganizations);
        var periods = await SafeResultAsync(periodsTask, () => SamplePeriods(organizationId));
        var activities = await SafeResultAsync(activitiesTask, SampleActivities);
        var tasks = await SafeResultAsync(tasksTask, SampleTasks);

        var quickActions = BuildQuickActions(periodId);
        return new DashboardOverview(organizations, periods, activities, tasks, quickActions);
    }

    private IReadOnlyList<QuickAction> BuildQuickActions(Guid? periodId)
    {
        var periodQuery = periodId.HasValue ? $"?periodId={periodId}" : string.Empty;
        return new List<QuickAction>
        {
            new("Start ny rapport", "Åbn rapportwizard og forbered årets ESG-indberetning", "bi bi-rocket", $"/wizard{periodQuery}", "Start"),
            new("Importer data", "Upload energi-, vand- eller affaldsdata via CSV", "bi bi-upload", "/imports", "Importer"),
            new("Gennemse metrics", "Dyk ned i trends for emissioner og diversitet", "bi bi-graph-up-arrow", "/metrics", "Se dashboard"),
        };
    }

    private static IReadOnlyList<OrganizationSummary> SampleOrganizations()
        => new[]
        {
            new OrganizationSummary(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Nordic Renewables", "Active", "Energy", 4),
            new OrganizationSummary(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Green Manufacturing A/S", "Active", "Manufacturing", 2),
            new OrganizationSummary(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Sustainability Hub", "Active", "Consulting", 6),
        };

    private static IReadOnlyList<ReportingPeriodSummary> SamplePeriods(Guid? organizationId)
        => new[]
        {
            new ReportingPeriodSummary(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "2023", "In progress", DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), "Maja Holm", 0.62),
            new ReportingPeriodSummary(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "2022", "Locked", DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)), "Maja Holm", 1),
        };

    private static IReadOnlyList<ActivitySummary> SampleActivities()
        => new[]
        {
            new ActivitySummary("Periode 2023 opdateret", DateTimeOffset.UtcNow.AddHours(-3), "Jonas Madsen", 0.5),
            new ActivitySummary("Energi-import færdiggjort", DateTimeOffset.UtcNow.AddDays(-1), "Sofie Kragh", 1),
            new ActivitySummary("Nye indikatorer tilføjet", DateTimeOffset.UtcNow.AddDays(-2), "AI Copilot", 0.75),
        };

    private static IReadOnlyList<TaskSummary> SampleTasks()
        => new[]
        {
            new TaskSummary(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Godkend energidata", "Energi", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)), "Jonas", "Open", "/tasks/energy"),
            new TaskSummary(Guid.Parse("55555555-5555-5555-5555-555555555555"), "Upload HR-data", "HR", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), "Liva", "Open", "/tasks/hr"),
            new TaskSummary(Guid.Parse("66666666-6666-6666-6666-666666666666"), "Afklar afvigelse", "Governance", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), "Noah", "Overdue", "/tasks/issue"),
        };

    private async Task<IReadOnlyList<T>> SafeResultAsync<T>(Task<IReadOnlyList<T>> task, Func<IReadOnlyList<T>> fallback)
    {
        try
        {
            var result = await task.ConfigureAwait(false);
            if (result is { Count: > 0 })
            {
                return result;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or NotSupportedException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Falling back to sample data for {Type}", typeof(T).Name);
        }

        return fallback();
    }
}
