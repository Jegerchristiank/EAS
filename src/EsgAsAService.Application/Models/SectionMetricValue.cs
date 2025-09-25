namespace EsgAsAService.Application.Models;

public record SectionMetricValue(
    string Section,
    string Metric,
    double? NumericValue,
    string? TextValue,
    string? Unit,
    string? Notes
);
