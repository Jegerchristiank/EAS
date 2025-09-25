using System.Text.Json.Serialization;

namespace EsgAsAService.Application.Models;

public class SectionMetricSet
{
    [JsonPropertyName("section")] public string Section { get; set; } = string.Empty;
    [JsonPropertyName("metrics")] public Dictionary<string, MetricValue> Metrics { get; set; } = new();
}

public class MetricValue
{
    [JsonPropertyName("value")] public double? Value { get; set; }
    [JsonPropertyName("text")] public string? Text { get; set; }
    [JsonPropertyName("unit")] public string? Unit { get; set; }
    [JsonPropertyName("source")] public string? Source { get; set; }
    [JsonPropertyName("formula")] public string? Formula { get; set; }
    [JsonPropertyName("notes")] public string? Notes { get; set; }
}
