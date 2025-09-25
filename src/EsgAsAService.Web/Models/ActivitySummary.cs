namespace EsgAsAService.Web.Models;

public sealed record ActivitySummary(
    string Description,
    DateTimeOffset OccurredAt,
    string Actor,
    double Progress)
{
    public string OccurredDisplay => OccurredAt.ToLocalTime().ToString("dd-MM-yyyy HH:mm");
    public int ProgressPercent => (int)Math.Clamp(Math.Round(Progress * 100, MidpointRounding.AwayFromZero), 0, 100);
}
