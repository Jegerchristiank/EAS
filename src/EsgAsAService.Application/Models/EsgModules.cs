using System;
using System.Text.Json.Serialization;

namespace EsgAsAService.Application.Models;

public class EsgModuleDefinition
{
    [JsonPropertyName("module")] public string Module { get; set; } = string.Empty;
    [JsonPropertyName("section_code")] public string SectionCode { get; set; } = string.Empty;
    [JsonPropertyName("section_name")] public string SectionName { get; set; } = string.Empty;
    [JsonPropertyName("datapoints")] public List<EsgModuleDatapoint> Datapoints { get; set; } = new();
}

public class EsgModuleDatapoint
{
    [JsonPropertyName("key")] public string Key { get; set; } = string.Empty;
    [JsonPropertyName("label")] public string Label { get; set; } = string.Empty;
    [JsonPropertyName("datatype")] public string? DataType { get; set; }
    [JsonPropertyName("unit")] public string? Unit { get; set; }
    [JsonPropertyName("allowed_values")] public IReadOnlyList<string> AllowedValues { get; set; } = Array.Empty<string>();
    [JsonPropertyName("requirement")] public string? Requirement { get; set; }
    [JsonPropertyName("depends_on")] public string? DependsOn { get; set; }
    [JsonPropertyName("notes")] public string? Notes { get; set; }
}
