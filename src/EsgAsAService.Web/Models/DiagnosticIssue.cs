namespace EsgAsAService.Web.Models;

public sealed record DiagnosticIssue(
    string Code,
    string Severity,
    string Message,
    DateTimeOffset CreatedAt)
{
    public string CreatedDisplay => CreatedAt.ToLocalTime().ToString("dd-MM-yyyy HH:mm");
}
