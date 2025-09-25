using System.ComponentModel.DataAnnotations;

namespace EsgAsAService.Web.Models;

public sealed record OrganizationSummary(
    Guid Id,
    [property: Required]
    string Name,
    string Status,
    string? Sector,
    int ActiveTaskCount)
{
    public static OrganizationSummary Placeholder(string name, string status = "Active", string? sector = null, int tasks = 0)
        => new(Guid.Empty, name, status, sector, tasks);
}
