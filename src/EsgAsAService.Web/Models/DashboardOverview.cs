namespace EsgAsAService.Web.Models;

public sealed record DashboardOverview(
    IReadOnlyList<OrganizationSummary> ActiveOrganizations,
    IReadOnlyList<ReportingPeriodSummary> ReportingPeriods,
    IReadOnlyList<ActivitySummary> RecentActivities,
    IReadOnlyList<TaskSummary> OpenTasks,
    IReadOnlyList<QuickAction> QuickActions);
