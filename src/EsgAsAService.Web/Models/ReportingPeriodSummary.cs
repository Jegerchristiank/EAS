namespace EsgAsAService.Web.Models;

public sealed record ReportingPeriodSummary(
    Guid Id,
    string Name,
    string Status,
    DateOnly? Deadline,
    string? Owner,
    double Completion)
{
    public string DeadlineDisplay => Deadline?.ToString("yyyy-MM-dd") ?? "-";
    public int CompletionPercent => (int)Math.Clamp(Math.Round(Completion * 100, MidpointRounding.AwayFromZero), 0, 100);
}
