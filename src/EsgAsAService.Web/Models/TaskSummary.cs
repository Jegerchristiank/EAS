namespace EsgAsAService.Web.Models;

public sealed record TaskSummary(
    Guid Id,
    string Title,
    string Category,
    DateOnly? DueDate,
    string Assignee,
    string Status,
    string? Link)
{
    public string DueDisplay => DueDate?.ToString("dd-MM-yyyy") ?? "-";
}
