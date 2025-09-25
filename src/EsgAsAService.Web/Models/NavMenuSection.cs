namespace EsgAsAService.Web.Models;

public sealed record NavMenuSection(
    string Title,
    string Icon,
    IReadOnlyList<NavMenuItem> Items,
    string? Description = null);

public sealed record NavMenuItem(
    string Title,
    string Icon,
    string Href,
    string? BadgeKey = null,
    string? Tooltip = null,
    bool RequiresAuthentication = true);
