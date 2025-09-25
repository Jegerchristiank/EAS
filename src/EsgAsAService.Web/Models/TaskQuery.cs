namespace EsgAsAService.Web.Models;

public sealed record TaskQuery(
    Guid? OrganizationId,
    Guid? PeriodId,
    string? Status,
    string? Scope,
    string? Owner = null);
