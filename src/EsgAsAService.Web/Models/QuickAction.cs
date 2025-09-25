namespace EsgAsAService.Web.Models;

public sealed record QuickAction(
    string Title,
    string Description,
    string Icon,
    string Href,
    string CtaText);
