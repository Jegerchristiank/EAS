using System.Collections.Generic;
using System.Text.Json;
using EsgAsAService.Application.Calculations;
using EsgAsAService.Domain.Calculations;
using EsgAsAService.Domain.Factors;
using EsgAsAService.Domain.Input;
using Xunit;

namespace EsgAsAService.Tests.Unit;

public class ModuleCalculationTests
{
    private static ModuleResult RunModule(string sectionCode, EsgInputPayload payload, FactorSet factors)
    {
        var registry = ModuleFunctions.CreateDefaultRegistry();
        var descriptor = registry.Get(sectionCode);
        var context = new ModuleCalculationContext(
            payload,
            factors,
            new CalculationAssumptions(new Dictionary<string, string>()),
            new Dictionary<string, ModuleResult>());

        return descriptor.Invoke(context);
    }

    private static EsgSectionInput Section(string code, params (string key, EsgFieldValue value)[] fields)
    {
        var fieldDict = new Dictionary<string, EsgFieldValue>();
        foreach (var (key, value) in fields)
        {
            fieldDict[key] = value;
        }

        return new EsgSectionInput(code, code, code, fieldDict);
    }

    [Fact]
    public void RunB3_DerivesScope2FromElectricityFactor()
    {
        var sections = new Dictionary<string, EsgSectionInput>
        {
            ["B3"] = Section("B3",
                ("scope1_ton", EsgFieldValue.FromNumber(0m)),
                ("scope2_ton", EsgFieldValue.FromNumber(0m)),
                ("scope3_ton", EsgFieldValue.FromNumber(0m)),
                ("el_ikke_vedvarende_mwh", EsgFieldValue.FromNumber(120m)))
        };

        var payload = new EsgInputPayload(sections, new Dictionary<string, string>());
        var factors = new FactorSet(
            "v2025.1",
            new Dictionary<string, EmissionFactorDefinition>(
            {
                ["electricity.dk.location"] = new EmissionFactorDefinition(
                    name: "electricity.dk.location",
                    unit: "kgCO2e/kWh",
                    value: 0.192m,
                    source: "Energinet",
                    region: "DK",
                    year: 2024,
                    validFrom: null,
                    validTo: null,
                    methodology: "")
            }),
            new Dictionary<string, EmissionFactorDefinition>());

        var result = RunModule("B3", payload, factors);

        var expected = 120m * 1000m * 0.192m / 1000m;
        Assert.Equal(expected, result.ValueRaw);
        Assert.Contains("electricity.dk.location", result.Trace);
        Assert.DoesNotContain("Samlet CO₂e er 0", string.Join(";", result.Warnings));
    }

    [Fact]
    public void RunC3_ScoresTransitionPlan()
    {
        var sections = new Dictionary<string, EsgSectionInput>
        {
            ["C3"] = Section("C3",
                ("reduktionsmaal", new EsgFieldValue.ObjectArray(new[] { JsonDocumentFactory.CreateDocument(new { target = "Scope 1" }) })),
                ("baseline_aar", EsgFieldValue.FromNumber(2020m)),
                ("handlinger_liste", EsgFieldValue.FromStringArray(new[] { "Tiltag" })),
                ("omstillingsplan_beskrivelse", EsgFieldValue.FromText("Plan")))
        };

        var payload = new EsgInputPayload(sections, new Dictionary<string, string>());
        var factors = new FactorSet("v2025.1", new Dictionary<string, EmissionFactorDefinition>(), new Dictionary<string, EmissionFactorDefinition>());

        var result = RunModule("C3", payload, factors);

        Assert.Equal(100m, result.ValueRaw);
        Assert.Contains("score", result.Trace);
    }

    private static class JsonDocumentFactory
    {
        public static JsonDocument CreateDocument(object value)
        {
            var json = JsonSerializer.Serialize(value);
            return JsonDocument.Parse(json);
        }
    }
}
