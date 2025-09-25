using EsgAsAService.Application.Models;
using EsgAsAService.Infrastructure.Data;
using EsgAsAService.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Infrastructure.Reporting;

public static class FullReportEnricher
{
    public static async Task PopulateFromV2Async(EsgDbContext db, FullEsgReport rep, Guid periodId, CancellationToken ct)
    {
        var period = await db.ReportingPeriodsV2.AsNoTracking().FirstOrDefaultAsync(p => p.Id == periodId, ct);
        if (period is null) return;

        var manualInputs = await db.SectionMetricInputs.AsNoTracking()
            .Where(x => x.ReportingPeriodId == periodId)
            .ToListAsync(ct);
        var manualLookup = manualInputs.ToDictionary(
            x => (x.Section.ToUpperInvariant(), x.Metric.ToUpperInvariant()),
            x => x
        );

        SectionMetricSet EnsureSet(string section)
        {
            if (!rep.SectionMetrics.TryGetValue(section, out var set))
            {
                set = new SectionMetricSet { Section = section };
                rep.SectionMetrics[section] = set;
            }
            return set;
        }

        SectionMetricInput? Manual(string section, string metric)
        {
            manualLookup.TryGetValue((section.ToUpperInvariant(), metric.ToUpperInvariant()), out var value);
            return value;
        }

        double? RegisterNumeric(string section, string metric, double? computedValue, string? unit, string formulaId, string? extraNotes = null)
        {
            var manual = Manual(section, metric);
            double? final = manual?.NumericValue ?? computedValue;
            var finalUnit = manual?.Unit ?? unit;
            string source = manual is not null ? "manual" : computedValue.HasValue ? "calculated" : "not_available";
            string? notes = extraNotes;
            if (manual is not null)
            {
                if (!string.IsNullOrWhiteSpace(manual.Notes))
                {
                    notes = string.IsNullOrWhiteSpace(notes) ? manual.Notes : string.Concat(notes, " | ", manual.Notes);
                }
                if (computedValue.HasValue)
                {
                    var baseline = $"Baseline: {computedValue.Value:0.###}{(unit is null ? string.Empty : " " + unit)}";
                    notes = string.IsNullOrWhiteSpace(notes) ? baseline : string.Concat(notes, " | ", baseline);
                }
            }

            EnsureSet(section).Metrics[metric] = new MetricValue
            {
                Value = final,
                Unit = finalUnit,
                Source = source,
                Formula = formulaId,
                Notes = notes
            };

            return final;
        }

        string? RegisterText(string section, string metric, string? computedText, string formulaId, string? extraNotes = null)
        {
            var manual = Manual(section, metric);
            string? final = manual?.TextValue ?? computedText;
            string source = manual?.TextValue is not null ? "manual" : !string.IsNullOrWhiteSpace(computedText) ? "calculated" : "not_available";
            string? notes = extraNotes;
            if (manual is not null && !string.IsNullOrWhiteSpace(manual.Notes))
            {
                notes = string.IsNullOrWhiteSpace(notes) ? manual.Notes : string.Concat(notes, " | ", manual.Notes);
            }

            EnsureSet(section).Metrics[metric] = new MetricValue
            {
                Text = final,
                Source = source,
                Formula = formulaId,
                Notes = notes
            };

            return final;
        }

        // B3 emissions
        var v2rows = await (from cr in db.CalculationResults
                            join se in db.ScopeEntries on cr.ScopeEntryId equals se.Id
                            join act in db.Activities on se.ActivityId equals act.Id
                            where act.ReportingPeriodId == periodId
                            select new { se.Scope, cr.Co2eKg, act.Category }).ToListAsync(ct);
        double? computedScope1 = null, computedScope2 = null, computedScope3 = null, computedIntensity = null;
        var carriers = new List<B3CarrierItem>();
        if (v2rows.Count > 0)
        {
            computedScope1 = v2rows.Where(x => x.Scope == 1).Sum(x => x.Co2eKg);
            computedScope2 = v2rows.Where(x => x.Scope == 2).Sum(x => x.Co2eKg);
            computedScope3 = v2rows.Where(x => x.Scope == 3).Sum(x => x.Co2eKg);
            carriers = v2rows
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Category) ? "(unspecified)" : x.Category)
                .Select(g => new B3CarrierItem { Carrier = g.Key, Co2eKg = g.Sum(x => x.Co2eKg) })
                .OrderByDescending(x => x.Co2eKg)
                .ToList();
            var fin = await db.Financials.AsNoTracking().FirstOrDefaultAsync(f => f.ReportingPeriodId == periodId, ct);
            if (fin is not null && fin.Revenue > 0)
            {
                var total = (computedScope1 ?? 0) + (computedScope2 ?? 0) + (computedScope3 ?? 0);
                computedIntensity = total / fin.Revenue;
            }
        }
        var scope1 = RegisterNumeric("B3", "scope1_kg", computedScope1, "kg", "B3.scope1_kg");
        var scope2 = RegisterNumeric("B3", "scope2_kg", computedScope2, "kg", "B3.scope2_kg");
        var scope3 = RegisterNumeric("B3", "scope3_kg", computedScope3, "kg", "B3.scope3_kg");
        var totalFinal = RegisterNumeric("B3", "total_kg", (scope1 ?? 0) + (scope2 ?? 0) + (scope3 ?? 0), "kg", "B3.total_kg");
        var intensity = RegisterNumeric("B3", "intensity_kg_per_revenue", computedIntensity, "kg/LCU", "B3.intensity_kg_per_revenue");

        rep.B3 = new B3Section
        {
            Scope1Kg = scope1 ?? 0,
            Scope2Kg = scope2 ?? 0,
            Scope3Kg = scope3 ?? 0,
            TotalKg = totalFinal ?? 0,
            IntensityKgPerRevenue = intensity,
            ByCarrier = carriers
        };

        // B4 pollution
        var pollution = await db.PollutionRegisters.AsNoTracking()
            .Where(p => p.ReportingPeriodId == periodId)
            .ToListAsync(ct);
        Dictionary<Guid, string>? unitCodes = null;
        if (pollution.Count > 0)
        {
            var unitIds = pollution.Select(p => p.UnitId).Distinct().ToList();
            unitCodes = await db.Units.AsNoTracking()
                .Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Code, ct);
        }
        rep.B4 = pollution.Select(p => new B4PollutionItem
        {
            Substance = p.Substance,
            Quantity = p.Quantity,
            UnitId = p.UnitId,
            UnitCode = unitCodes is not null && unitCodes.TryGetValue(p.UnitId, out var code) ? code : null,
            ReportingSystem = p.ReportingSystem,
            ReportingId = p.ReportingId
        }).ToList();
        RegisterNumeric("B4", "record_count", pollution.Count, null, "B4.record_count");
        RegisterNumeric("B4", "quantity_total", pollution.Sum(p => p.Quantity), pollution.Count > 0 ? pollution.First().UnitId.ToString() : null, "B4.quantity_total");
        var topSubstance = pollution.OrderByDescending(p => p.Quantity).FirstOrDefault()?.Substance;
        RegisterText("B4", "top_substance", topSubstance, "B4.top_substance");

        // B5 biodiversity
        var locations = rep.B1?.Locations;
        if (locations is null || locations.Count == 0)
        {
            locations = await db.Locations.AsNoTracking()
                .Where(l => l.OrganisationId == period.OrganisationId)
                .Select(l => new B1Location
                {
                    Id = l.Id,
                    Name = l.Name,
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    InSensitiveArea = l.InSensitiveArea,
                    Note = l.SensitiveAreaNote
                }).ToListAsync(ct);
            if (rep.B1 is null)
            {
                rep.B1 = new B1Section { Locations = locations };
            }
            else
            {
                rep.B1.Locations = locations;
            }
        }
        var sensitiveCount = locations.Count(l => l.InSensitiveArea);
        RegisterNumeric("B5", "total_locations", locations.Count, null, "B5.total_locations");
        RegisterNumeric("B5", "sensitive_locations", sensitiveCount, null, "B5.sensitive_locations");
        rep.B5 = locations.Select(l => new B5BiodiversityItem { Id = l.Id, InSensitiveArea = l.InSensitiveArea, SensitiveAreaNote = l.Note }).ToList();

        // B6 water
        var water = await db.WaterMeters.AsNoTracking().Where(w => w.ReportingPeriodId == periodId).ToListAsync(ct);
        var intake = RegisterNumeric("B6", "intake_m3", water.Count > 0 ? water.Sum(m => m.IntakeM3) : null, "m3", "B6.intake_m3");
        var discharge = RegisterNumeric("B6", "discharge_m3", water.Count > 0 ? water.Sum(m => m.DischargeM3 ?? 0) : null, "m3", "B6.discharge_m3");
        var consumption = RegisterNumeric("B6", "consumption_m3", (intake ?? 0) - (discharge ?? 0), "m3", "B6.consumption_m3");
        rep.B6 = new B6WaterSection { IntakeM3 = intake ?? 0, DischargeM3 = discharge ?? 0, ConsumptionM3 = consumption ?? 0 };

        // B7 resources & waste
        var wastes = await db.WasteManifests.AsNoTracking().Where(w => w.ReportingPeriodId == periodId).ToListAsync(ct);
        var materials = await db.MaterialFlows.AsNoTracking().Where(m => m.ReportingPeriodId == periodId).ToListAsync(ct);
        var wasteTotal = RegisterNumeric("B7", "waste_total_kg", wastes.Count > 0 ? wastes.Sum(w => w.QuantityKg) : null, "kg", "B7.waste_total_kg");
        var materialTotal = RegisterNumeric("B7", "material_total_tonnes", materials.Count > 0 ? materials.Sum(m => m.QuantityTonnes) : null, "tonnes", "B7.material_total_tonnes");
        rep.B7 = new B7ResourcesSection
        {
            Waste = wastes.Select(w => new B7WasteItem { EakCode = w.EakCode, QuantityKg = w.QuantityKg, Disposition = w.Disposition }).ToList(),
            Materials = materials.Select(m => new B7MaterialItem { Material = m.Material, QuantityTonnes = m.QuantityTonnes }).ToList()
        };

        // B8 workforce
        var headcounts = await db.HRHeadcounts.AsNoTracking().Where(h => h.ReportingPeriodId == periodId).ToListAsync(ct);
        var computedFte = headcounts.Count > 0 ? headcounts.Sum(x => x.FteTotal) : (double?)null;
        var fteTotal = RegisterNumeric("B8", "fte_total", computedFte, "fte", "B8.fte_total");
        var turnover = RegisterNumeric("B8", "turnover_rate", null, null, "B8.turnover_rate");
        rep.B8 = new B8WorkforceSection { FteTotal = fteTotal ?? 0, Headcount = headcounts.Cast<object>().ToList(), TurnoverRate = turnover };

        // B9 safety
        var safety = await db.SafetyIncidents.AsNoTracking().Where(s => s.ReportingPeriodId == periodId).ToListAsync(ct);
        if (safety.Count > 0)
        {
            var incidents = RegisterNumeric("B9", "incidents", safety.Sum(x => (double)x.IncidentsCount), null, "B9.incidents");
            var hours = RegisterNumeric("B9", "hours_worked", safety.Sum(x => x.HoursWorked), "hours", "B9.hours_worked");
            double? afr = null;
            if (incidents.HasValue && hours.HasValue && hours.Value > 0)
            {
                afr = (incidents.Value / hours.Value) * 200000.0;
            }
            afr = RegisterNumeric("B9", "afr", afr, "rate", "B9.afr");
            rep.B9 = new B9SafetySection { IncidentsCount = incidents ?? 0, HoursWorked = hours ?? 0, AccidentFrequency = afr };
        }

        // B10 payroll/training
        var payroll = await db.HRPayrolls.AsNoTracking().FirstOrDefaultAsync(p => p.ReportingPeriodId == periodId, ct);
        var training = await db.HRTrainings.AsNoTracking().FirstOrDefaultAsync(t => t.ReportingPeriodId == periodId, ct);
        double? computedGenderGap = (payroll is null || !payroll.AvgSalaryFemale.HasValue || !payroll.AvgSalaryMale.HasValue || payroll.AvgSalaryMale == 0)
            ? null
            : (payroll.AvgSalaryFemale!.Value / payroll.AvgSalaryMale!.Value) - 1.0;
        var genderGap = RegisterNumeric("B10", "gender_pay_gap", computedGenderGap, null, "B10.gender_pay_gap");
        var coverage = RegisterNumeric("B10", "coverage_pct", payroll?.CollectiveAgreementCoveragePct, null, "B10.coverage_pct");
        double? trainingPerEmployee = null;
        if (training?.TotalTrainingHours is not null && (fteTotal ?? 0) > 0)
        {
            trainingPerEmployee = training.TotalTrainingHours / (fteTotal ?? 1);
        }
        trainingPerEmployee = RegisterNumeric("B10", "training_hours_per_employee", trainingPerEmployee, "hours", "B10.training_hours_per_employee");

        rep.B10 = new B10PayTrainingSection
        {
            GenderPayGap = genderGap,
            CoveragePct = coverage,
            TrainingHoursPerEmployee = trainingPerEmployee
        };

        // B11 governance cases
        var cases = await db.GovernanceCases.AsNoTracking().Where(g => g.ReportingPeriodId == periodId).ToListAsync(ct);
        if (cases.Count > 0)
        {
            RegisterNumeric("B11", "case_count", cases.Count, null, "B11.case_count");
            RegisterNumeric("B11", "total_amount", cases.Sum(c => c.Amount ?? 0), "LCU", "B11.total_amount");
            rep.B11 = cases.Select(c => new B11GovernanceCase { Type = c.Type, Outcome = c.Outcome, Amount = c.Amount, CaseRef = c.CaseRef }).ToList();
        }

        // C1–C9 narratives (textual metrics recorded for provenance)
        var strategy = await db.StrategyTargets.AsNoTracking().FirstOrDefaultAsync(s => s.ReportingPeriodId == periodId, ct);
        if (strategy is not null)
        {
            rep.C1 = new C1StrategySection
            {
                Summary = RegisterText("C1", "summary", strategy.Summary, "C1.summary"),
                ShortTermTarget = RegisterText("C1", "short_term_target", strategy.ShortTermTarget, "C1.short_term_target"),
                LongTermTarget = RegisterText("C1", "long_term_target", strategy.LongTermTarget, "C1.long_term_target"),
                EmissionReductionTargetPct = RegisterNumeric("C1", "emission_reduction_target_pct", strategy.EmissionReductionTargetPct, "%", "C1.emission_reduction_target_pct"),
                TargetYear = (int?)RegisterNumeric("C1", "target_year", strategy.TargetYear, null, "C1.target_year"),
                InvestmentPlan = RegisterText("C1", "investment_plan", strategy.InvestmentPlan, "C1.investment_plan"),
                Progress = RegisterText("C1", "progress", strategy.Progress, "C1.progress")
            };
        }

        var risk = await db.RiskAssessments.AsNoTracking().FirstOrDefaultAsync(r => r.ReportingPeriodId == periodId, ct);
        if (risk is not null)
        {
            rep.C2 = new C2RiskSection
            {
                Process = RegisterText("C2", "process", risk.Process, "C2.process"),
                ClimateRisks = RegisterText("C2", "climate_risks", risk.ClimateRisks, "C2.climate_risks"),
                Opportunities = RegisterText("C2", "opportunities", risk.Opportunities, "C2.opportunities"),
                TimeHorizon = RegisterText("C2", "time_horizon", risk.TimeHorizon, "C2.time_horizon"),
                Mitigations = RegisterText("C2", "mitigations", risk.Mitigations, "C2.mitigations")
            };
        }

        var humanRights = await db.HumanRightsAssessments.AsNoTracking().FirstOrDefaultAsync(h => h.ReportingPeriodId == periodId, ct);
        if (humanRights is not null)
        {
            rep.C3 = new C3HumanRightsSection
            {
                PolicyExists = RegisterNumeric("C3", "policy_exists", humanRights.PolicyExists ? 1 : 0, "bool", "C3.policy_exists") == 1,
                DueDiligenceInPlace = RegisterNumeric("C3", "due_diligence_in_place", humanRights.DueDiligenceInPlace ? 1 : 0, "bool", "C3.due_diligence_in_place") == 1,
                HighRiskAreas = RegisterText("C3", "high_risk_areas", humanRights.HighRiskAreas, "C3.high_risk_areas"),
                Remediation = RegisterText("C3", "remediation", humanRights.Remediation, "C3.remediation"),
                TrainingProvided = RegisterText("C3", "training_provided", humanRights.TrainingProvided, "C3.training_provided")
            };
        }

        var governance = await db.GovernanceOversights.AsNoTracking().FirstOrDefaultAsync(g => g.ReportingPeriodId == periodId, ct);
        if (governance is not null)
        {
            rep.C4 = new C4GovernanceSection
            {
                BoardOversight = RegisterText("C4", "board_oversight", governance.BoardOversight, "C4.board_oversight"),
                ManagementResponsibilities = RegisterText("C4", "management_responsibilities", governance.ManagementResponsibilities, "C4.management_responsibilities"),
                Incentives = RegisterText("C4", "incentives", governance.Incentives, "C4.incentives"),
                ClimateExpertOnBoard = RegisterNumeric("C4", "climate_expert_on_board", governance.ClimateExpertOnBoard is true ? 1 : 0, "bool", "C4.climate_expert_on_board") == 1
            };
        }

        var board = await db.BoardDiversities.AsNoTracking().FirstOrDefaultAsync(b => b.ReportingPeriodId == periodId, ct);
        if (board is not null)
        {
            rep.C5 = new C5BoardDiversitySection
            {
                PercentFemale = RegisterNumeric("C5", "percent_female", board.PercentFemale, "%", "C5.percent_female"),
                PercentMale = RegisterNumeric("C5", "percent_male", board.PercentMale, "%", "C5.percent_male"),
                PercentOther = RegisterNumeric("C5", "percent_other", board.PercentOther, "%", "C5.percent_other"),
                PercentIndependent = RegisterNumeric("C5", "percent_independent", board.PercentIndependent, "%", "C5.percent_independent"),
                DiversityPolicy = RegisterText("C5", "diversity_policy", board.DiversityPolicy, "C5.diversity_policy"),
                SelectionProcess = RegisterText("C5", "selection_process", board.SelectionProcess, "C5.selection_process")
            };
        }

        var stakeholder = await db.StakeholderEngagements.AsNoTracking().FirstOrDefaultAsync(s => s.ReportingPeriodId == periodId, ct);
        if (stakeholder is not null)
        {
            rep.C6 = new C6StakeholderSection
            {
                StakeholderGroups = RegisterText("C6", "stakeholder_groups", stakeholder.StakeholderGroups, "C6.stakeholder_groups"),
                EngagementProcess = RegisterText("C6", "engagement_process", stakeholder.EngagementProcess, "C6.engagement_process"),
                KeyTopics = RegisterText("C6", "key_topics", stakeholder.KeyTopics, "C6.key_topics"),
                WorkerRepresentation = RegisterNumeric("C6", "worker_representation", stakeholder.WorkerRepresentation is true ? 1 : 0, "bool", "C6.worker_representation") == 1
            };
        }

        var valueChain = await db.ValueChainCoverages.AsNoTracking().FirstOrDefaultAsync(v => v.ReportingPeriodId == periodId, ct);
        if (valueChain is not null)
        {
            rep.C7 = new C7ValueChainSection
            {
                UpstreamCoverage = RegisterText("C7", "upstream_coverage", valueChain.UpstreamCoverage, "C7.upstream_coverage"),
                DownstreamCoverage = RegisterText("C7", "downstream_coverage", valueChain.DownstreamCoverage, "C7.downstream_coverage"),
                Scope3Categories = RegisterText("C7", "scope3_categories", valueChain.Scope3Categories, "C7.scope3_categories"),
                DataGaps = RegisterText("C7", "data_gaps", valueChain.DataGaps, "C7.data_gaps")
            };
        }

        var assurance = await db.AssuranceActivities.AsNoTracking().FirstOrDefaultAsync(a => a.ReportingPeriodId == periodId, ct);
        if (assurance is not null)
        {
            RegisterText("C8", "provider", assurance.Provider, "C8.provider");
            RegisterText("C8", "assurance_level", assurance.AssuranceLevel, "C8.assurance_level");
            RegisterText("C8", "scope", assurance.Scope, "C8.scope");
            RegisterNumeric("C8", "is_independent", assurance.IsIndependent is true ? 1 : 0, "bool", "C8.is_independent");
            RegisterText("C8", "summary", assurance.Summary, "C8.summary");
            var dateText = assurance.AssuranceDate?.ToString("yyyy-MM-dd");
            RegisterText("C8", "assurance_date", dateText, "C8.assurance_date");
            rep.C8 = new C8AssuranceSection
            {
                Provider = assurance.Provider,
                AssuranceLevel = assurance.AssuranceLevel,
                Scope = assurance.Scope,
                AssuranceDate = assurance.AssuranceDate,
                Summary = assurance.Summary,
                IsIndependent = assurance.IsIndependent
            };
        }

        var methodology = await db.MethodologyStatements.AsNoTracking().FirstOrDefaultAsync(m => m.ReportingPeriodId == periodId, ct);
        if (methodology is not null)
        {
            rep.C9 = new C9MethodologySection
            {
                ReportingBoundary = RegisterText("C9", "reporting_boundary", methodology.ReportingBoundary, "C9.reporting_boundary"),
                ConsolidationApproach = RegisterText("C9", "consolidation_approach", methodology.ConsolidationApproach, "C9.consolidation_approach"),
                EmissionFactorSources = RegisterText("C9", "emission_factor_sources", methodology.EmissionFactorSources, "C9.emission_factor_sources"),
                EstimationApproach = RegisterText("C9", "estimation_approach", methodology.EstimationApproach, "C9.estimation_approach"),
                MaterialityThreshold = RegisterText("C9", "materiality_threshold", methodology.MaterialityThreshold, "C9.materiality_threshold")
            };
        }
    }
}
