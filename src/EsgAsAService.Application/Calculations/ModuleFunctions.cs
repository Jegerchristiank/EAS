using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using EsgAsAService.Domain.Calculations;
using EsgAsAService.Domain.Factors;
using EsgAsAService.Domain.Input;

namespace EsgAsAService.Application.Calculations;

/// <summary>
/// Factory helpers for registering module calculation delegates. Real formulas are implemented per module file.
/// </summary>
public static class ModuleFunctions
{
    private static readonly JsonSerializerOptions TraceSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static ModuleFunctionRegistry CreateDefaultRegistry()
    {
        var modules = new List<ModuleFunctionDescriptor>
        {
            Module("Basismodul", "B1", BSeries.RunB1),
            Module("Basismodul", "B2", BSeries.RunB2),
            Module("Basismodul", "B3", BSeries.RunB3),
            Module("Basismodul", "B4", BSeries.RunB4),
            Module("Basismodul", "B5", BSeries.RunB5),
            Module("Basismodul", "B6", BSeries.RunB6),
            Module("Basismodul", "B7", BSeries.RunB7),
            Module("Basismodul", "B8", BSeries.RunB8),
            Module("Basismodul", "B9", BSeries.RunB9),
            Module("Basismodul", "B10", BSeries.RunB10),
            Module("Basismodul", "B11", BSeries.RunB11),
            Module("Udvidet", "C1", CSeries.RunC1),
            Module("Udvidet", "C2", CSeries.RunC2),
            Module("Udvidet", "C3", CSeries.RunC3),
            Module("Udvidet", "C4", CSeries.RunC4),
            Module("Udvidet", "C5", CSeries.RunC5),
            Module("Udvidet", "C6", CSeries.RunC6),
            Module("Udvidet", "C7", CSeries.RunC7),
            Module("Udvidet", "C8", CSeries.RunC8),
            Module("Udvidet", "C9", CSeries.RunC9)
        };

        return new ModuleFunctionRegistry(modules);
    }

    private static ModuleFunctionDescriptor Module(string module, string sectionCode, Func<ModuleCalculationContext, ModuleResult> func)
        => new(module, sectionCode, func);

    private static ModuleResult BuildResult(
        ModuleCalculationContext context,
        string sectionCode,
        decimal rawValue,
        string unit,
        string method,
        IEnumerable<string> sources,
        object tracePayload,
        IEnumerable<string>? warnings = null)
    {
        var section = context.Input.GetSection(sectionCode);
        var rounded = Math.Round(rawValue, 2, MidpointRounding.AwayFromZero);
        var trace = JsonSerializer.Serialize(tracePayload, TraceSerializerOptions);
        var warningArray = warnings?.Where(w => !string.IsNullOrWhiteSpace(w)).Distinct().ToArray() ?? Array.Empty<string>();

        return new ModuleResult(
            Module: section.Module,
            SectionCode: sectionCode,
            ValueRaw: rawValue,
            Unit: unit,
            Method: method,
            Sources: sources.Distinct().ToArray(),
            Trace: trace,
            ValueRounded: rounded,
            Warnings: warningArray);
    }

    private static bool TryDeriveEnergyEmissions(
        ModuleCalculationContext context,
        EsgSectionInput section,
        string datapointKey,
        string factorKey,
        List<object> factorTraces,
        out decimal derivedTons)
    {
        derivedTons = 0m;
        var energyMWh = section.GetDecimal(datapointKey);
        if (energyMWh <= 0)
        {
            return false;
        }

        if (!context.Factors.TryResolve(factorKey, out var factor))
        {
            return false;
        }

        var energyKWh = energyMWh * 1000m;
        var kilograms = energyKWh * factor.Value;
        derivedTons = kilograms / 1000m;

        factorTraces.Add(new
        {
            factorKey,
            context.Factors.Version,
            factor.Unit,
            factor.Value,
            datapointKey,
            energyMWh,
            energyKWh,
            kilograms,
            derivedTons
        });

        return true;
    }

    private static ModuleResult Placeholder(ModuleCalculationContext context, string sectionCode)
    {
        return BuildResult(
            context,
            sectionCode,
            0m,
            string.Empty,
            "not_implemented",
            Array.Empty<string>(),
            new { message = "Calculation not yet implemented." },
            new[] { "Module calculation pending implementation." });
    }

    private static class BSeries
    {
        public static ModuleResult RunB1(ModuleCalculationContext context)
        {
            var section = context.Input.GetSection("B1");
            var employees = section.GetDecimal("antal_ansatte");
            var balance = section.GetDecimal("balancesum_eur");
            var revenue = section.GetDecimal("omsaetning_eur");
            var siteCount = section.GetArrayLength("sites");
            var certificateCount = section.GetArrayLength("certifikater_miljoemaerker");

            var warnings = new List<string>();
            if (employees <= 0)
            {
                warnings.Add("B1.antal_ansatte mangler eller er 0.");
            }
            if (siteCount == 0)
            {
                warnings.Add("B1.sites er tom – verificér lokationsdata.");
            }

            return BuildResult(
                context,
                "B1",
                employees,
                "personer",
                "Direkte wizard-input (antal_ansatte)",
                new[] { "B1.antal_ansatte", "B1.omsaetning_eur", "B1.balancesum_eur" },
                new
                {
                    employees,
                    revenueEUR = revenue,
                    balanceEUR = balance,
                    siteCount,
                    certificateCount
                },
                warnings);
        }

        public static ModuleResult RunB2(ModuleCalculationContext context)
        {
            var section = context.Input.GetSection("B2");
            var policyKeys = section.Fields.Keys
                .Where(k => k.EndsWith("_har_politik", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var total = policyKeys.Count;
            var active = policyKeys.Count(key => section.GetBoolean(key));
            var share = total > 0 ? active * 100m / total : 0m;

            var publicKeys = section.Fields.Keys.Where(k => k.EndsWith("_offentlig", StringComparison.OrdinalIgnoreCase)).ToList();
            var publicTotal = publicKeys.Count;
            var publicAvailable = publicKeys.Count(key => section.GetBoolean(key));

            var warnings = new List<string>();
            if (total == 0)
            {
                warnings.Add("Ingen politik-flag fundet i B2.");
            }

            return BuildResult(
                context,
                "B2",
                share,
                "%",
                "Andel af politikområder med aktiv politik",
                policyKeys,
                new
                {
                    activePolicyCount = active,
                    policyCount = total,
                    publicStatements = publicAvailable,
                    publicStatementsCount = publicTotal,
                    sharePercent = share
                },
                warnings);
        }

        public static ModuleResult RunB3(ModuleCalculationContext context)
        {
            var section = context.Input.GetSection("B3");
            var scope1 = section.GetDecimal("scope1_ton");
            var scope2 = section.GetDecimal("scope2_ton");
            var scope3 = section.GetDecimal("scope3_ton");
            var factorTrace = new List<object>();

            if (scope1 <= 0 && TryDeriveEnergyEmissions(context, section, "braendstof_ikke_vedvarende_mwh", "fuel.generic.mwh", factorTrace, out var derivedScope1))
            {
                scope1 = derivedScope1;
            }

            if (scope2 <= 0 && TryDeriveEnergyEmissions(context, section, "el_ikke_vedvarende_mwh", "electricity.dk.location", factorTrace, out var derivedScope2))
            {
                scope2 = derivedScope2;
            }

            if (scope2 <= 0 && TryDeriveEnergyEmissions(context, section, "anden_energi_mwh", "district_heat.dk", factorTrace, out var derivedDistrict))
            {
                scope2 = derivedDistrict;
            }

            var total = scope1 + scope2 + scope3;

            var warnings = new List<string>();
            if (total <= 0)
            {
                warnings.Add("Samlet CO₂e er 0 – verificér B3 scope-input.");
            }
            else if (scope2 <= 0 && section.GetDecimal("el_ikke_vedvarende_mwh") > 0 && factorTrace.Count == 0)
            {
                warnings.Add("B3: Mangler emissionsfaktor for elektricitet (electricity.dk.location).");
            }

            return BuildResult(
                context,
                "B3",
                total,
                "ton CO2e",
                "Summation af scope 1-3",
                new[] { "B3.scope1_ton", "B3.scope2_ton", "B3.scope3_ton" },
                new
                {
                    scope1,
                    scope2,
                    scope3,
                    total,
                    factors = factorTrace
                },
                warnings);
        }

        public static ModuleResult RunB4(ModuleCalculationContext context)
        {
            var section = context.Input.GetSection("B4");
            var quantityKg = section.GetDecimal("forurening_maengde");
            var type = section.GetString("forurening_type");
            var medium = section.GetString("forurening_medium");

            var warnings = new List<string>();
            if (quantityKg <= 0)
            {
                warnings.Add("B4.forurening_maengde er 0 – angiv væsentligste udledningsmængde.");
            }

            return BuildResult(
                context,
                "B4",
                quantityKg,
                "kg",
                "Direkte wizard-input (forurening_maengde)",
                new[] { "B4.forurening_maengde" },
                new
                {
                    type,
                    medium,
                    quantityKg
                },
                warnings);
        }

        public static ModuleResult RunB5(ModuleCalculationContext context)
        {
            var section = context.Input.GetSection("B5");
            if (!section.TryGetDecimal("arealforbrug_aendring_pct", out var changePct))
            {
                var lastYear = section.GetDecimal("arealforbrug_seneste_aar_hektar");
                var reportYear = section.GetDecimal("arealforbrug_rapportaar_hektar");
                changePct = lastYear > 0
                    ? (reportYear - lastYear) * 100m / lastYear
                    : 0m;
            }

            var sensitiveLocations = section.GetArrayLength("bio_følsomme_omraader");

            var warnings = new List<string>();
            if (sensitiveLocations == 0)
            {
                warnings.Add("Ingen registrerede biodiversitetsfølsomme områder (B5.bio_følsomme_omraader).");
            }

            return BuildResult(
                context,
                "B5",
                changePct,
                "%",
                "Rapporteret procentvis ændring i arealforbrug",
                new[] { "B5.arealforbrug_aendring_pct" },
                new
                {
                    changePct,
                    sensitiveLocationCount = sensitiveLocations
                },
                warnings);
        }

        public static ModuleResult RunB6(ModuleCalculationContext context)
        {
            var section = context.Input.GetSection("B6");
            decimal consumption;
            if (section.TryGetDecimal("vand_forbrug_m3", out var reportedConsumption) && reportedConsumption > 0)
            {
                consumption = reportedConsumption;
            }
            else
            {
                var withdrawal = section.GetDecimal("vand_udtagning_m3");
                consumption = Math.Max(0m, withdrawal);
            }

            return BuildResult(
                context,
                "B6",
                consumption,
                "m3",
                "Direkte wizard-input eller udledning fra vand_udtagning_m3",
                new[] { "B6.vand_forbrug_m3", "B6.vand_udtagning_m3" },
                new
                {
                    consumption,
                    reported = reportedConsumption,
                    withdrawal = section.GetDecimal("vand_udtagning_m3")
                });
        }

        public static ModuleResult RunB7(ModuleCalculationContext context)
        {
            var section = context.Input.GetSection("B7");
            var totalHazardous = section.GetDecimal("affald_total_farligt_ton");
            var totalNonHazardous = section.GetDecimal("affald_total_ikke_farligt_ton");
            var recycle = section.GetDecimal("affald_til_genbrug_genanvendelse_ton");
            var total = totalHazardous + totalNonHazardous;
            var recycleRate = total > 0 ? recycle * 100m / total : 0m;

            var warnings = new List<string>();
            if (total <= 0)
            {
                warnings.Add("B7 affaldsmængder er 0 – verificér data.");
            }

            return BuildResult(
                context,
                "B7",
                recycleRate,
                "%",
                "Genbrugsandel af samlet affald",
                new[]
                {
                    "B7.affald_total_farligt_ton",
                    "B7.affald_total_ikke_farligt_ton",
                    "B7.affald_til_genbrug_genanvendelse_ton"
                },
                new
                {
                    totalHazardous,
                    totalNonHazardous,
                    total,
                    recycle,
                    recycleRate,
                    cirkulær = section.GetBoolean("cirkulaer_ja")
                },
                warnings);
        }

        public static ModuleResult RunB8(ModuleCalculationContext context)
        {
            var section = context.Input.GetSection("B8");
            if (!section.TryGetDecimal("medarbejderomsaetning_pct", out var turnover))
            {
                var totalEmployees = section.GetDecimal("kontrakttype_total");
                var tempEmployees = section.GetDecimal("kontrakttype_midlt");
                turnover = totalEmployees > 0 ? tempEmployees * 100m / totalEmployees : 0m;
            }

            var male = section.GetDecimal("koen_maend");
            var female = section.GetDecimal("koen_kvinder");
            var other = section.GetDecimal("koen_andet") + section.GetDecimal("koen_ikke_reg");
            var totalGender = male + female + other;

            return BuildResult(
                context,
                "B8",
                turnover,
                "%",
                "Rapporteret medarbejderomsætning",
                new[] { "B8.medarbejderomsaetning_pct", "B8.kontrakttype_midlt", "B8.kontrakttype_total" },
                new
                {
                    turnover,
                    male,
                    female,
                    other,
                    totalGender
                });
        }

        public static ModuleResult RunB9(ModuleCalculationContext context)
        {
            var section = context.Input.GetSection("B9");
            if (!section.TryGetDecimal("ulykker_frekvens", out var frequency))
            {
                var accidents = section.GetDecimal("ulykker_antal");
                var employees = context.Input.TryGetSection("B8")?.GetDecimal("kontrakttype_total") ?? 0m;
                frequency = employees > 0 ? accidents * 100m / employees : 0m;
            }

            var fatalities = section.GetDecimal("doedsfald_skade") + section.GetDecimal("doedsfald_helbred");

            return BuildResult(
                context,
                "B9",
                frequency,
                "pr. 100 fuldtidsansatte",
                "Rapporteret ulykkesfrekvens",
                new[] { "B9.ulykker_frekvens", "B9.ulykker_antal" },
                new
                {
                    frequency,
                    fatalities,
                    accidents = section.GetDecimal("ulykker_antal")
                });
        }

        public static ModuleResult RunB10(ModuleCalculationContext context)
        {
            var section = context.Input.GetSection("B10");
            var gap = section.GetDecimal("loenforskel_maend_kvinder_pct");

            var coverage = section.GetDecimal("overenskomst_daeckning_pct");
            var trainingFemale = section.GetDecimal("uddannelsestimer_kvinder");
            var trainingMale = section.GetDecimal("uddannelsestimer_maend");
            var trainingOther = section.GetDecimal("uddannelsestimer_andre");

            return BuildResult(
                context,
                "B10",
                gap,
                "%",
                "Rapporteret lønforskel",
                new[] { "B10.loenforskel_maend_kvinder_pct" },
                new
                {
                    gap,
                    coverage,
                    trainingFemale,
                    trainingMale,
                    trainingOther
                });
        }

        public static ModuleResult RunB11(ModuleCalculationContext context)
        {
            var section = context.Input.GetSection("B11");
            var totalFines = section.GetDecimal("boeder_samlet_kroner");
            var caseCount = section.GetDecimal("domme_antal");

            return BuildResult(
                context,
                "B11",
                totalFines,
                "DKK",
                "Samlet bødestørrelse",
                new[] { "B11.boeder_samlet_kroner" },
                new
                {
                    totalFines,
                    caseCount
                });
        }
    }

    private static class CSeries
    {
        public static ModuleResult RunC1(ModuleCalculationContext context)
        {
            var section = context.Input.GetSection("C1");
            var total = section.Fields.Count;
            var filledKeys = section.Fields.Keys
                .Where(section.HasValue)
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var missingKeys = section.Fields.Keys
                .Except(filledKeys, StringComparer.OrdinalIgnoreCase)
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var completion = total > 0 ? filledKeys.Count * 100m / total : 0m;

            var warnings = new List<string>();
            if (completion < 60m)
            {
                warnings.Add("C1 narrativer er under 60% udfyldt – gennemgå strategifeltet.");
            }

            return BuildResult(
                context,
                "C1",
                completion,
                "%",
                "Narrativ fuldstændighedsscore",
                section.Fields.Keys,
                new
                {
                    completion,
                    filledKeys,
                    missingKeys
                },
                warnings);
        }

        public static ModuleResult RunC2(ModuleCalculationContext context)
        {
            var section = context.Input.GetSection("C2");
            var total = section.Fields.Count;
            var filled = section.Fields.Keys.Count(section.HasValue);
            var completion = total > 0 ? filled * 100m / total : 0m;

            return BuildResult(
                context,
                "C2",
                completion,
                "%",
                "Risikoafsnit fuldstændighed",
                section.Fields.Keys,
                new
                {
                    completion,
                    filled,
                    total
                });
        }

        public static ModuleResult RunC3(ModuleCalculationContext context)
        {
            var section = context.Input.GetSection("C3");
            var targets = section.GetArrayLength("reduktionsmaal");
            var actions = section.GetArrayLength("handlinger_liste");
            var hasPlan = section.HasValue("omstillingsplan_beskrivelse");
            var baselineYear = section.GetDecimal("baseline_aar");

            var score = 0m;
            if (targets > 0) score += 40m;
            if (baselineYear > 0) score += 20m;
            if (actions > 0) score += 20m;
            if (hasPlan) score += 20m;

            return BuildResult(
                context,
                "C3",
                score,
                "point",
                "Klimaomstillingsscore",
                new[] { "reduktionsmaal", "baseline_aar", "handlinger_liste", "omstillingsplan_beskrivelse" },
                new
                {
                    score,
                    targets,
                    actions,
                    hasPlan,
                    baselineYear
                });
        }

        public static ModuleResult RunC4(ModuleCalculationContext context)
        {
            var section = context.Input.GetSection("C4");
            var adaptation = section.GetBoolean("tilpasning_ja");
            var hasRiskDescription = section.HasValue("klimarisici_beskrivelse");
            var hasExposure = section.HasValue("eksponering_sårbarhed");
            var hasFinancial = section.HasValue("finansiel_påvirkning_beskrivelse");

            var readiness = 0m;
            if (hasRiskDescription) readiness += 25m;
            if (hasExposure) readiness += 25m;
            if (adaptation) readiness += 25m;
            if (hasFinancial) readiness += 25m;

            return BuildResult(
                context,
                "C4",
                readiness,
                "point",
                "Risiko- og tilpasningsscore",
                new[] { "klimarisici_beskrivelse", "eksponering_sårbarhed", "tilpasning_ja", "finansiel_påvirkning_beskrivelse" },
                new
                {
                    readiness,
                    adaptation,
                    hasRiskDescription,
                    hasExposure,
                    hasFinancial,
                    timeHorizon = section.GetString("tidshorisont")
                });
        }

        public static ModuleResult RunC5(ModuleCalculationContext context)
        {
            var section = context.Input.GetSection("C5");
            var ratio = section.GetDecimal("forhold_kvinder_maend_ledelsesniveau");
            var independents = section.GetDecimal("selvstaendige_antal");
            var temps = section.GetDecimal("vikarer_antal");

            return BuildResult(
                context,
                "C5",
                ratio,
                "ratio",
                "Bestyrelsesdiversitet",
                new[] { "forhold_kvinder_maend_ledelsesniveau", "selvstaendige_antal", "vikarer_antal" },
                new
                {
                    ratio,
                    independents,
                    temps
                });
        }

        public static ModuleResult RunC6(ModuleCalculationContext context)
        {
            var section = context.Input.GetSection("C6");
            var policy = section.GetBoolean("code_of_conduct_eller_mr_politik");
            var grievance = section.GetBoolean("klagemekanisme_ansatte");
            var process = section.HasValue("mr_processer_beskrivelse");

            var score = 0m;
            if (policy) score += 50m;
            if (grievance) score += 30m;
            if (process) score += 20m;

            return BuildResult(
                context,
                "C6",
                score,
                "point",
                "Menneskerettighedsstyring",
                new[] { "code_of_conduct_eller_mr_politik", "klagemekanisme_ansatte", "mr_processer_beskrivelse" },
                new
                {
                    score,
                    policy,
                    grievance,
                    process
                });
        }

        public static ModuleResult RunC7(ModuleCalculationContext context)
        {
            var section = context.Input.GetSection("C7");
            var ownIncidents = section.HasValue("haendelser_egen_arbejdsstyrke");
            var chainIncidents = section.HasValue("haendelser_vaerdikaede");
            var totalIncidents = (ownIncidents ? 1 : 0) + (chainIncidents ? 1 : 0);

            var actionsOwn = section.HasValue("handlinger_egen_arbejdsstyrke");
            var actionsChain = section.HasValue("handlinger_vaerdikaede");

            return BuildResult(
                context,
                "C7",
                totalIncidents,
                "cases",
                "Antal rapporterede hændelsesområder",
                new[] { "haendelser_egen_arbejdsstyrke", "haendelser_vaerdikaede", "handlinger_egen_arbejdsstyrke", "handlinger_vaerdikaede" },
                new
                {
                    totalIncidents,
                    ownIncidents,
                    chainIncidents,
                    actionsOwn,
                    actionsChain
                });
        }

        public static ModuleResult RunC8(ModuleCalculationContext context)
        {
            var section = context.Input.GetSection("C8");
            var revenueKeys = new[]
            {
                "indtægt_kontroversielle_vaaben_dkk",
                "indtægt_tobak_dkk",
                "indtægt_kul_dkk",
                "indtægt_olie_dkk",
                "indtægt_gas_dkk",
                "indtægt_pesticid_kemi_dkk"
            };
            var exposure = revenueKeys.Sum(key => section.GetDecimal(key));

            var benchmarkKeys = new[]
            {
                "eu_benchmark_kul_over1pct",
                "eu_benchmark_olie_over10pct",
                "eu_benchmark_gas_over50pct",
                "eu_benchmark_el_100g_over50pct"
            };

            var benchmarks = benchmarkKeys.ToDictionary(k => k, k => section.GetBoolean(k));

            return BuildResult(
                context,
                "C8",
                exposure,
                "DKK",
                "Summation af kontroversielle indtægter",
                revenueKeys.Concat(benchmarkKeys),
                new
                {
                    exposure,
                    breakdown = revenueKeys.ToDictionary(k => k, k => section.GetDecimal(k)),
                    benchmarks
                });
        }

        public static ModuleResult RunC9(ModuleCalculationContext context)
        {
            var section = context.Input.GetSection("C9");
            var ratio = section.GetDecimal("koens_forhold_best");

            return BuildResult(
                context,
                "C9",
                ratio,
                "ratio",
                "Kønsforhold øverste ledelsesorgan",
                new[] { "koens_forhold_best" },
                new
                {
                    ratio
                });
        }
    }
}
