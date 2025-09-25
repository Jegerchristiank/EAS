# ESG Metrics & Formula Catalogue

This reference documents the quantitative and qualitative calculations that underpin the ESG-as-a-Service platform.  Each subsection maps directly to the ESRS/CSRD structure (B1–B11 and C1–C9) and specifies:

- **Metric code** — short id used in software (e.g. `B3.total_kg`).
- **Formula** — deterministic rule or aggregation applied.
- **Inputs** — data sources (tables, user inputs, or derived records).
- **Overrides** — optional user supplied values captured via `SectionMetricInput`.
- **Output** — the displayed value (numeric or textual) used for JSON and PDF reports.

Where metrics cannot be computed from canonical activity data, a user input is required.  All overrides are persisted per period via the `/v1/metrics/{periodId}` endpoint and surfaced with provenance in the generated report.

---

## B1 — Company & Basis

| Metric | Formula | Inputs | Overrides | Notes |
| --- | --- | --- | --- | --- |
| `B1.location_count` | Count of active locations attached to organisation | `Locations` | ✖ | Displayed as contextual text |
| `B1.sensitive_location_count` | Count of locations with `InSensitiveArea = true` | `Locations` | ✖ | Supports biodiversity narrative |
| `B1.certificate_count` | Count of active certificates | `Certificates` | ✖ | Used to highlight certifications |

## B2 — Policies

Binary policy flags stored in `PolicyRegisters`. No numeric formulae beyond boolean casting. Narrative summarises goal/status/milestones.

## B3 — Energy & Emissions

| Metric | Formula | Inputs | Overrides | Notes |
| --- | --- | --- | --- | --- |
| `B3.scope1_kg` | Σ CO₂e where `Scope = 1` | `CalculationResults` ↔ `ScopeEntries` | ✔ | Override via `B3.scope1_kg` |
| `B3.scope2_kg` | Σ CO₂e where `Scope = 2` | same as above | ✔ | |
| `B3.scope3_kg` | Σ CO₂e where `Scope = 3` | same | ✔ | |
| `B3.total_kg` | `scope1 + scope2 + scope3` | derived | ✔ | Manual total supersedes component sum |
| `B3.intensity_kg_per_revenue` | `total_kg / Financials.Revenue` | `Financials` | ✔ (`B3.intensity`) | Requires revenue > 0 |

## B4 — Pollution

| Metric | Formula | Inputs | Overrides | Notes |
| --- | --- | --- | --- | --- |
| `B4.record_count` | Count of active `PollutionRegisters` rows | table | ✖ | |
| `B4.quantity_total` | Σ `Quantity` | table | ✔ (`B4.quantity_total`) | |
| `B4.top_substance` | Substance with highest quantity | table | ✖ | Displayed textually |

## B5 — Biodiversity

| Metric | Formula | Inputs | Overrides | Notes |
| --- | --- | --- | --- | --- |
| `B5.sensitive_locations` | Count of `Locations.InSensitiveArea = true` | `Locations` | ✔ (`B5.sensitive_locations`) | |
| `B5.total_locations` | Count of locations | `Locations` | ✖ | |

## B6 — Water

| Metric | Formula | Inputs | Overrides | Notes |
| --- | --- | --- | --- | --- |
| `B6.intake_m3` | Σ `WaterMeters.IntakeM3` | `WaterMeters` | ✔ (`B6.intake_m3`) | |
| `B6.discharge_m3` | Σ `COALESCE(DischargeM3,0)` | table | ✔ (`B6.discharge_m3`) | |
| `B6.consumption_m3` | `intake_m3 - discharge_m3` | derived | ✔ (`B6.consumption_m3`) | negative values clipped to 0 for presentation |

## B7 — Resources & Waste

| Metric | Formula | Inputs | Overrides | Notes |
| --- | --- | --- | --- | --- |
| `B7.waste_total_kg` | Σ `WasteManifests.QuantityKg` | table | ✔ (`B7.waste_total_kg`) | |
| `B7.material_total_tonnes` | Σ `MaterialFlows.QuantityTonnes` | table | ✔ (`B7.material_total_tonnes`) | |
| `B7.waste_breakdown` | Group by `Disposition` | table | ✖ | Displayed in PDF table |

## B8 — Workforce

| Metric | Formula | Inputs | Overrides | Notes |
| --- | --- | --- | --- | --- |
| `B8.fte_total` | Σ `HRHeadcounts.FteTotal` | table | ✔ | |
| `B8.turnover_rate` | User input (mandatory) | `SectionMetricInput` (`B8.turnover_rate`) | ✔ | Supply as decimal (0.12 = 12%) |

## B9 — Health & Safety

| Metric | Formula | Inputs | Overrides | Notes |
| --- | --- | --- | --- | --- |
| `B9.incidents` | Σ `SafetyIncidents.IncidentsCount` | table | ✔ (`B9.incidents`) | |
| `B9.hours_worked` | Σ `SafetyIncidents.HoursWorked` | table | ✔ (`B9.hours_worked`) | |
| `B9.afr` | `(incidents / hours_worked) × 200,000` | derived | ✔ (`B9.afr`) | Requires hours>0 |

## B10 — Pay & Training

| Metric | Formula | Inputs | Overrides | Notes |
| --- | --- | --- | --- | --- |
| `B10.gender_pay_gap` | `(AvgSalaryFemale / AvgSalaryMale) - 1` | `HRPayrolls` | ✔ (`B10.gender_pay_gap`) | Null if male salary missing |
| `B10.coverage_pct` | `HRPayrolls.CollectiveAgreementCoveragePct` | table | ✔ | |
| `B10.training_hours_per_employee` | `HRTrainings.TotalTrainingHours / FTE_total` | `HRTrainings`, `HRHeadcounts` | ✔ | |

## B11 — Business Conduct

| Metric | Formula | Inputs | Overrides | Notes |
| --- | --- | --- | --- | --- |
| `B11.case_count` | Count of `GovernanceCases` | table | ✔ (`B11.case_count`) | |
| `B11.total_amount` | Σ `GovernanceCases.Amount` | table | ✔ (`B11.total_amount`) | |

## C1 — Strategy & Targets

Qualitative narrative captured in `StrategyTargets`. Quantitative helpers:

| Metric | Formula | Inputs | Overrides |
| --- | --- | --- | --- |
| `C1.emission_reduction_target_pct` | Stored field | `StrategyTargets` | ✔ (`C1.emission_reduction_target_pct`) |
| `C1.target_year` | Stored field | table | ✔ |

## C2 — Risks

Narrative text from `RiskAssessments`. Optional manual metrics for risk scoring can be provided via `SectionMetricInput` (e.g. `C2.risk_score`).

## C3 — Human Rights

- Boolean flags (`PolicyExists`, `DueDiligenceInPlace`) derive directly from table.
- Additional textual metrics (`HighRiskAreas`, `Remediation`, `TrainingProvided`).

## C4 — Governance

Narrative fields plus optional `C4.expert_on_board` boolean.

## C5 — Board Diversity

| Metric | Formula | Inputs |
| --- | --- | --- |
| `C5.percent_female` | stored decimal (0–1) | `BoardDiversities` |
| `C5.percent_independent` | stored decimal | `BoardDiversities` |

## C6 — Stakeholders

Textual narrative. Optional boolean `WorkerRepresentation` used and surfaced.

## C7 — Value Chain

Narrative describing upstream/downstream coverage and Scope 3 categories.

## C8 — Assurance

- Provider, level, scope, independence captured in `AssuranceActivities`.
- Assurance date shown if present.

## C9 — Methodology

Narrative fields outlining boundary, consolidation, emission factor sources, estimation methods and materiality threshold.

---

### Manual Metric Overrides

- Stored in `SectionMetricInput` with composite key `(Section, Metric)`.
- Supplied via `PUT /v1/metrics/{periodId}`.
- Accepts numeric (`Value`) and textual (`Text`) overrides with optional unit/notes.
- During report composition the system prefers manual values; original computed values remain documented in the `notes` field for transparency.

Each metric listed above references a logical code of the form `{Section}.{metric}`.  The PDF renderer displays provenance (`calculated` vs. `manual`) and naming consistent with this catalogue.

