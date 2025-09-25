ESG-as-a-Service — TODO (arbejdsliste)

Denne liste samler forestående ændringer og forbedringer på tværs af løsningen. Brug checkbokse som løbende status, og opret issues/PRs pr. punkt hvor det giver mening.

## Arkitektur & Platform
- [x] Konsolider .NET 9 mål og fjern resterende 8.x artefakter (runtimeconfig, bin/obj-levn)
- [x] Indfør Directory.Packages.props til central versionstyring af NuGet-pakker
- [x] Skift til AddDbContextPool for EF Core (connection pooling)
- [ ] Etabler modulære kataloger for V1 vs V2 domæne (eller fusionér – se Domænemodel)
- [x] Indfør feature flags (f.eks. Microsoft.FeatureManagement) til gradvis aktivering af nye flows
 - [x] Opret `.gitignore` for bin/obj, *.db, *.log, *.pid, TestResults/, node_modules
 - [x] Fjern ubrugte scaffold-filer og -projekter (fx `WebApp1`)
 - [x] Fjern tomme placeholder-klasser (`Class1.cs` i Application/Domain/Infrastructure)

## API & Controllers
 - [x] Gennemgå alle Created(...) → brug CreatedAtAction/Created(Uri, ...) konsistent
   - [x] Import invoice endpoints returnerer 201 Created med Location til process‑rute
- [x] Standardiser fejlmodel (ProblemDetails) og valideringsfejl (kode/field/message)
- [ ] Tilføj XML-docs på API-modeller og endpoints (Swagger viser beskrivelser)
- [x] Versionér API (/v1, vnd.media+json) og plan for breaking changes
- [ ] Indfør konsistente DTO’er (request/response) fremfor domæne-entity direkte i controllers
 - [x] Integrationstest for `POST /reports/generate/json` (200 OK, 404 period, 501 service)
 - [x] `POST /reports/generate/json` returnerer 404 for ukendt `periodId`

## Domænemodel & Data
- [ ] Fusionér V1 (ReportingPeriod, EnvironmentalActivity) og V2 (ReportingPeriodV2, ScopeEntry) eller læg klar migrationsplan
- [ ] Ryd op i dobbelte tabeller og relationer (V1/V2) via migrations
- [x] Fjern EnsureCreated i API-opstart (brug kun `Database.Migrate()`)
- [ ] Hærde opstartssekvens (retry/backoff, idempotent seed, bedre fejlmeldinger)
 - [x] Indeksér hyppige opslag (SQLite dev): opretter indexes på Activities/ScopeEntries/CalculationResults/Water/Waste/Materials ved opstart
- [ ] Overvej EF compiled queries for tunge lister/summeringer
- [ ] Audit: flyt custom WriteAuditLogs() til EF-interceptor, og udvid med før/efter-diffs på udvalgte entiteter

## Sikkerhed
- [ ] Gennemgå RBAC-politikker og ressource-niveau checks (org/period id-tilhørsforhold)
- [ ] Evidence upload: virus scanning/hardening (ClamAV/komp. service) og MIME-sniffing
 - [x] CSP, HSTS, X-Content-Type-Options, Referrer-Policy i Web (prod)
- [ ] Secret-håndtering: sikre at API keys kun lever i user-secrets/ENV, ikke i filer
 - [x] Rate limiting: differentier per endpoint-type (ingest-policy for imports)
- [ ] Log redaction af PII/hemmeligheder (structured logging-fields)

## Ydeevne
- [ ] Cache af emissionsfaktorer (IEmissionFactorCache) – tidsstyret invalidation og warmup-strategi
- [ ] Optimer lister med projektioner (Select) og AsNoTracking() konsistent
- [ ] Batch-indsæt ved imports (EF AddRangeAsync + SaveChanges i chunk)
- [ ] Profilér hot paths (dotnet-trace, PerfView) og adresser N+1 queries

## Observability
- [x] OpenTelemetry: baseline traces + metrics (ASP.NET Core, EF Core, HttpClient)
- [ ] Standardiser ILogger brug (LoggerMessage source generators for hot paths)
- [x] Health checks: readiness (`/health`) inkl. DB
- [ ] Korrelér DiagnosticService-koder i logs med request-id/span-id

## Web/Blazor
- [ ] Wizard: validering og UX-meldinger (alle steps), loading-states konsistent
- [ ] Tilføj formaterings-helpers (tal/datoer) med invariant/locale-korrekt visning
- [ ] A11y: ARIA-roller, tastaturnavigation, kontrast
- [ ] Global fejlkomponent med support-kode og link til logdownload i dev

## Test & Kvalitet
- [x] Udvid integrationstests med TestServer (første sæt: health, reports(feature gate), imports invoice)
- [ ] E2E-tests (Playwright) for vigtigste flows (login, wizard, rapport-download)
- [ ] Dækning: mål/threshold i CI (coverlet + threshold fail)
- [ ] Stabilitet: fake clock/now for deterministiske testcases
- [ ] Ryd resterende analyzer-warnings (CA*, CS*) hvor værdiskabende

## DevOps / CI/CD
- [x] Basis CI workflow: build + test på push/PR
- [ ] Udvid GitHub Actions: matrix (Windows/Linux), release build, artifacts (PDF/XBRL samples)
- [x] Konsolider workflows til én pipeline (fjern dubletten `.github/workflows/dotnet.yml`)
- [x] Docker multi-stage builds (API/Web), compose for dev, healthchecks
- [ ] Miljøfiler for staging/prod (appsettings.{Env}.json) og secrets i CI
 - [x] Dependabot for NuGet/Actions; Snyk (evt. senere)

## Dokumentation
- [ ] Opdater README (arkitekturdiagram, dataflow, udvikler-quickstart, profiler)
- [ ] API.md: komplet endpoints med param/respons + eksempler
- [ ] MIGRATIONS.md: strategi for V1→V2 konsolidering og rollback
- [ ] SECURITY.md: trusselsmodel for upload, auth, rate limiting

## Internationalisering & Tilgængelighed
- [ ] I18n: centraliserede tekster, kultur-specifik formatering
- [ ] A11y: auto-tests i CI (axe) og manuelle acceptance-kriterier

## Tech-debt & Refaktorisering
- [ ] Gennemfør Uri-typer i options/DTO’er (OpenAiOptions, eksterne providers)
- [ ] Konsolider rapportgenerator (PDF/XBRL) til services med tydelige grænseflader
- [ ] Fjern ubrugte helpers/klasser og dødt kode
- [ ] Ensartet navngivning (underscores i offentlige medlemmer undgås; JsonPropertyName ved behov)

## Sprint – Næste skridt (kort liste)
- [ ] API-versionering + konsistent ProblemDetails/valideringsformat
- [ ] DTO-lag i controllers (ingen domæne-entities udadtil)
- [x] Feature flags (infrastruktur på plads; gating på endpoints pending)
- [ ] OpenTelemetry udbygning (dashboards, korrelation, baggage)
- [x] Docker multi-stage + compose for lokal E2E
- [ ] E2E tests (Playwright) for login → wizard → JSON rapport
- [ ] Konsolidering af V1/V2 domæne via migration plan

## Done (denne iteration)
- Ryddet build-artefakter (bin/obj) og lokale outputfiler
- Tilføjet XML-doc/kommentarer på centrale interfaces (IReportGenerator, IAITextGenerator, IUnitConversionService, IVsmeReportService, ICalculationRunner, IEmissionFactorCache)
- Opdateret README med Next Steps og lokale tips
- Bygget og kørt tests: alle 38 passerer på .NET 9

## Spikes / Prototyper
- [ ] Cloud storage til Evidence (S3/Azure Blob) med rigtige pre-signed URLs
- [ ] Emission factor providers: caching/koalescering, fallbackstrategi, timeouts/retries (Polly)
- [ ] Feature: rapportskabeloner og parameterisering (branding, noter)

---

Ved hver PR: link til denne TODO og marker feltet, eller flyt punktet til “Done” i projektboard.

## ESG‑Modul: Fuld kravopfyldelse (B1–B11, C1–C9)

1) To‑do (implementering i faser)
 - [x] Model: tilføj manglende tabeller/entiteter (skitseret i Domain)
 - [x] DbContext: DbSet<> + QueryFilters + indekser (grundlæggende)
 - [ ] Migrations: generér og kør (Dev: SQLite; Prod: valgfri RDBMS)
 - [x] API: CRUD første bølge (locations, policies, financials, water, waste, materials, hr, safety, governance, pollution)
 - [x] Metrics endpoints (emissions, water, accidents, gender pay gap)
 - [x] Rapport: JSON generator (B1–B11 baseline, C placeholders)
 - [ ] ETL: parsere (CSV/XLSX) og mapping til normaliserede tabeller
   - [x] Staging uploads for energi/vand/affald (gemmer filer på disk + metadata)
   - [x] Energi: første `process` endpoint (CSV → Activities + ScopeEntry)
   - [x] Vand/Affald: `process` endpoints (CSV → WaterMeter/WasteManifest)
    - [x] Idempotens: undgå dubletter via normaliseret linje‑payload (pr. dokument)
    - [x] Kryds-dokument deduplikering: tjek org/period/type + normaliseret payload
    - [ ] Migration: tilføj hash-kolonne + unik index for (org, period, type, hash)
    - [x] Enheds‑konvertering ved import (vand: L→m3, affald: t→kg)
    - [ ] Udvid enheds‑konvertering (m3↔L↔kL, g↔kg↔t, mm.) og flere kategorier
 - [ ] Views: emissions_s1s2, emissions_intensity, water_consumption, accident_frequency, gender_pay_gap (DB‑views)
 - [ ] Sikkerhed: styrk RBAC på nye endpoints + audit/PII‑redaction
 - [ ] Tests: flere API/integration + ETL mappings
 - [x] Dokumentation: README/MIGRATIONS/TODO opdateret

1a) Implementation Plan (detaljeret)
- [ ] Core model: design tables, relations, constraints, indexes; generate migrations.
- [ ] Normalization: unit catalog, conversion rules, unified emission factor model.
- [ ] ETL staging: add staging tables, parsers, mapping and idempotent upserts.
- [ ] Calculations: scope 1/2/3, intensity, water consumption, accidents, gender pay gap.
- [ ] Policy/governance: CRUD for policies, milestones, cases, certificates, omissions.
- [ ] Views: create materialized/computed views for KPIs (emissions, intensity, accidents).
- [ ] API: CRUD endpoints, import endpoints, report generation, download (JSON/PDF).
- [ ] Validation: business rules, “skal/skal hvis relevant/kan” marking in responses.
- [ ] Security: RBAC for all write ops; audit logging and PII redaction.
 - [x] Reporting: JSON report schema, PDF templates by section (B1–B11, C1–C9).
   - [x] Add Full ESG JSON DTO + service (`FullEsgReport`), feature‑gated endpoint
   - [x] PDF: Cover, TOC, B1/B2 skeleton, B3 with category breakdown table, page footers with numbers
   - [x] PDF: Fill B6 (water) and B7 (waste/materials) from V2 data
   - [x] PDF: Fill B4/B5/B8–B11 with real tables and notes
   - [ ] Branding: theme, colors, page header/footer with logo
   - [ ] Snapshot tests for key PDF sections (search for known text/values)
- [ ] Tests: unit (formulas), API (controllers), ETL (mappings), integration (end‑to‑end).
- [ ] Observability: logs for ETL + calc runs; health checks for DB/metrics.
- [ ] Docs: developer guide, API reference, data dictionary, import templates.

2) Databasemodel (udvidelser)
- org_unit (eksisterer delvist): legal_form, nace, cvr
- location(org_id, lat/lon, in_sensitive_area, note)
- certificate(org_id, standard, valid_from/to)
- policy_register(org_id, klima/miljø/cirkulær/leverandør/anti‑korruption/dataprivatliv/whistleblower, goal, status, next_milestone, confidentiality_omissions, omission_note)
- period(org_id, year, start/end, revenue, currency, market_based_enabled)
- unit, unit_conversion (eksisterer)
- emissions_factor (eksisterer V2; udvid med method/valid_from/to)
- energy_reading(org_id, period_id, carrier, qty, unit)
- scope_entry (eksisterer i V2; brug til granuleret tracking)
- water_meter(intake_m3, discharge_m3)
- waste_manifest(eak_code, qty_kg, disposition)
- material_flow(material, qty_tonnes)
- hr_headcount(fte_total/by gender/country)
- hr_payroll(avg_salary_by_gender, coverage_pct)
- hr_training(total_training_hours)
- safety_incident(incidents_count, hours_worked)
- governance_case(type, outcome, amount, ref)
- financials(revenue, currency)
- pollution_register(substance, qty, unit, reporting_system/id)
- views: emissions_s1s2, emissions_intensity, water_consumption, accident_frequency, gender_pay_gap

3) API‑ruter (oversigt)
- Organisations/Periods/Locations/Policies/Certificates/Units/Conversions
- Energi/Water/Waste/Materials/HR/Safety/Pollution/Governance/Financials
- Metrics: emissions, intensity, water, accidents, gender_pay_gap
- Reports: generate JSON/PDF, download

4) ETL (import)
- Staging tabeller + parsere pr. datakilde; idempotent upserts
- Skabeloner: energi, vand, affald, HR (headcount/payroll/training), finans
- Validering: ranges, enheder, datoer i periode, referencer, duplicates

5) Formler
- CO₂e: qty_norm × factor; scope1/2/3 totals; intensity = total_kg / revenue
- Vandforbrug: intake − discharge
- Affald: pr. EAK + disposition
- Turnover: terminations / avg_fte
- AFR: (incidents / hours) × 200,000
- Løngab: (avg_female / avg_male) − 1

6) Output (rapportfelter)
- B1: grundlag (juridisk, NACE, CVR, lokationer, certifikater, udeladelser)
- B2: politikker (booleans + mål/status/milepæl)
- B3: energi/CO₂e (scope1/2/3, total, intensitet, metode)
- B4: forurening (stof, mængde, kilde, ID)
- B5: biodiversitet (følsomt område flag + note)
- B6: vand (indtag/udledning/forbrug)
- B7: ressourcer/affald (EAK, kg, disposition; materialeflow)
- B8: arbejdsstyrke (FTE, køn, land, turnover)
- B9: arbejdsmiljø (ulykker, timer, AFR)
- B10: løn/overenskomst/uddannelse (løngab, dækning, timer pr. ansat)
- B11: virksomhedsadfærd (sager, bøder)
- C1–C9: strategi/mål, risici, menneskerettigheder, ledelses‑kønsfordeling m.m.
- org_unit
  - id (uuid, pk)
  - legal_name (text) [skal]
  - legal_form (text) [skal]
  - nace_code (text, length 4) [skal]
  - cvr (text, unique) [skal]
  - created_at (timestamptz), updated_at (timestamptz)
- location
  - id (uuid, pk)
  - org_id (uuid, fk org_unit.id)
  - name (text)
  - latitude (decimal(9,6))
  - longitude (decimal(9,6))
  - in_sensitive_area (bool) [skal hvis relevant]
  - sensitive_area_note (text) [kan]
- certificate
  - id (uuid, pk)
  - org_id (uuid, fk)
  - standard (text, e.g., ISO14001) [kan]
  - valid_from (date), valid_to (date)
- policy_register
  - id (uuid, pk)
  - org_id (uuid, fk)
  - policy_climate (bool) [skal]
  - policy_environment (bool) [skal]
  - policy_circular (bool) [skal]
  - policy_supply_chain (bool) [skal]
  - policy_anti_corruption (bool) [skal]
  - policy_data_privacy (bool) [skal]
  - policy_whistleblower (bool) [skal]
  - goal (text) [kan]
  - status (text) [kan]
  - next_milestone (text) [kan]
  - confidentiality_omissions (bool), omission_note (text)
- period
  - id (uuid, pk)
  - org_id (uuid, fk)
  - year (int) [skal]
  - start_date (date), end_date (date) [skal]
  - revenue_amount (numeric(18,2)) [skal for intensitet]
  - currency (char(3)) [kan]
  - market_based_enabled (bool) [kan]
- unit
  - id (uuid, pk)
  - code (text, unique, e.g., kWh, MWh, L, m3, kg, t) [skal]
  - name (text), is_si (bool)
- unit_conversion
  - id (uuid, pk)
  - from_unit_id (uuid, fk unit.id)
  - to_unit_id (uuid, fk unit.id)
  - factor (numeric(20,10), >0)
  - unique index (from_unit_id, to_unit_id)
- emissions_factor
  - id (uuid, pk)
  - country (char(2)) [kan]
  - year (int) [kan]
  - type (text, e.g., electricity, diesel, gas) [skal]
  - scope (smallint: 1,2,3) [skal]
  - unit_id (uuid, fk unit.id)
  - value_kgco2e_per_unit (numeric(20,10)) [skal]
  - method (text: location_based/market_based/other) [kan]
  - valid_from (date), valid_to (date) [kan]
  - source (text) [kan]
  - index (country, year, type, scope)
- energy_reading
  - id (uuid, pk)
  - org_id (uuid, fk)
  - period_id (uuid, fk)
  - location_id (uuid, fk) [kan]
  - carrier (text: electricity, diesel, heating_oil, gas, district_heating, etc.) [skal]
  - quantity (numeric(20,6)) [skal]
  - unit_id (uuid, fk unit.id) [skal]
  - market_instruments_purchased (bool) [kan]
  - source_doc (text: invoice, meter, fuel_receipt) [kan]
  - reading_date (date) [kan]
- scope_entry (optional for fine-grained tracking of activities → scope)
  - id (uuid, pk)
  - org_id (uuid, fk)
  - period_id (uuid, fk)
  - activity_category (text) [kan]
  - scope (smallint: 1,2,3) [skal]
  - quantity (numeric(20,6)) [skal]
  - unit_id (uuid, fk unit.id) [skal]
  - emissions_factor_id (uuid, fk emissions_factor.id) [kan]
  - co2e_kg (numeric(20,6)) [udledt]
- water_meter
  - id (uuid, pk)
  - org_id (uuid, fk), period_id (uuid, fk), location_id (uuid, fk)
  - intake_m3 (numeric(20,6)) [skal hvis relevant]
  - discharge_m3 (numeric(20,6)) [kan]
  - source_doc (text) [kan]
- waste_manifest
  - id (uuid, pk)
  - org_id (uuid, fk), period_id (uuid, fk), location_id (uuid, fk)
  - eak_code (text) [skal]
  - quantity_kg (numeric(20,6)) [skal]
  - disposition (text: recycle, reuse_prep, disposal) [skal]
  - carrier (text), manifest_id (text) [kan]
- material_flow
  - id (uuid, pk)
  - org_id (uuid, fk), period_id (uuid, fk)
  - material (text) [skal hvis relevant]
  - quantity_tonnes (numeric(20,6)) [skal]
  - source_doc (text) [kan]
- hr_headcount
  - id (uuid, pk)
  - org_id (uuid, fk), period_id (uuid, fk)
  - fte_total (numeric(12,2)) [skal]
  - fte_female (numeric(12,2)), fte_male (numeric(12,2)), fte_other (numeric(12,2)) [kan]
  - country_code (char(2)) [kan]
- hr_person (optional, if person-level available; otherwise use aggregates)
  - id (uuid, pk)
  - org_id (uuid, fk)
  - gender (text), country_code (char(2)), hire_date (date), term_date (date) [kan]
- hr_payroll
  - id (uuid, pk)
  - org_id (uuid, fk), period_id (uuid, fk)
  - avg_salary_female (numeric(18,2)) [kan]
  - avg_salary_male (numeric(18,2)) [kan]
  - collective_agreement_coverage_pct (numeric(5,2)) [kan]
- hr_training
  - id (uuid, pk)
  - org_id (uuid, fk), period_id (uuid, fk)
  - total_training_hours (numeric(12,2)) [kan]
- safety_incident
  - id (uuid, pk)
  - org_id (uuid, fk), period_id (uuid, fk)
  - incidents_count (int) [skal hvis relevant]
  - hours_worked (numeric(20,2)) [skal]
  - source_doc (text) [kan]
- governance_case
  - id (uuid, pk)
  - org_id (uuid, fk), period_id (uuid, fk)
  - type (text: corruption, bribery, etc.) [skal hvis relevant]
  - outcome (text: fine, judgment) [kan]
  - amount (numeric(18,2)) [kan]
  - case_ref (text) [kan]
- financials
  - id (uuid, pk)
  - org_id (uuid, fk), period_id (uuid, fk)
  - revenue (numeric(18,2)) [skal]
  - currency (char(3)) [kan]
- pollution_register (for B4)
  - id (uuid, pk)
  - org_id (uuid, fk), period_id (uuid, fk), location_id (uuid, fk)
  - substance (text) [skal hvis relevant]
  - quantity (numeric(20,6)) [skal]
  - unit_id (uuid, fk unit.id) [skal]
  - reporting_system (text) [kan], reporting_id (text) [kan]
- views (database views)
  - emissions_s1s2(period_id, scope1_kg, scope2_kg, scope3_kg)
  - emissions_intensity(period_id, total_kg, revenue, intensity_kg_per_revenue)
  - water_consumption(period_id, consumption_m3)
  - accident_frequency(period_id, afr)
  - gender_pay_gap(period_id, gap_pct)
