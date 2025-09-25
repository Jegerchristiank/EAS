using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EsgAsAService.Api.Models.Vsme;

public record VsmeDisclosure(object? Value, IReadOnlyList<string> Sources);

public record VsmeBasicReport(
    [property: JsonPropertyName("E1_TotalEmissions")] VsmeDisclosure E1TotalEmissions,
    [property: JsonPropertyName("E2_EnergyConsumption")] VsmeDisclosure E2EnergyConsumption,
    [property: JsonPropertyName("E3_RenewableShare")] VsmeDisclosure E3RenewableShare,
    [property: JsonPropertyName("E4_Scope1")] VsmeDisclosure E4Scope1,
    [property: JsonPropertyName("E5_Scope2")] VsmeDisclosure E5Scope2,
    [property: JsonPropertyName("E6_Scope3")] VsmeDisclosure E6Scope3,
    [property: JsonPropertyName("S1_WorkforceSize")] VsmeDisclosure S1WorkforceSize,
    [property: JsonPropertyName("S2_GenderDiversity")] VsmeDisclosure S2GenderDiversity,
    [property: JsonPropertyName("S3_HealthSafety")] VsmeDisclosure S3HealthSafety,
    [property: JsonPropertyName("G1_AntiCorruptionPolicy")] VsmeDisclosure G1AntiCorruptionPolicy,
    [property: JsonPropertyName("G2_DataPrivacyPolicy")] VsmeDisclosure G2DataPrivacyPolicy
);
