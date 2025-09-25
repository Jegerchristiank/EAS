using System.ComponentModel.DataAnnotations;

namespace EsgAsAService.Api.Models;

public record SectionMetricDto(
    string Section,
    string Metric,
    double? Value,
    string? Text,
    string? Unit,
    string? Notes
);

public record SectionMetricsResponse(IReadOnlyList<SectionMetricDto> Metrics);

public record SectionMetricUpsertEntry(
    [property:Required, MaxLength(10)] string Section,
    [property:Required, MaxLength(50)] string Metric,
    double? Value,
    string? Text,
    string? Unit,
    string? Notes
);

public record SectionMetricsUpsertRequest([property:Required] IReadOnlyList<SectionMetricUpsertEntry> Metrics);
