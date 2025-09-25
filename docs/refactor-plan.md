# ESG Refactor Plan — Wizard, Calculations, Deterministic PDF

## Scope & Non-Goals
- **In scope:** Dynamic multi-step wizard driven by CSV schema, full B1–B11 & C1–C9 calculations, deterministic QuestPDF report generation, factor management (auto/custom), schema coverage gate, CI updates, documentation overhaul.
- **Persisting:** Identity + 2FA, RBAC, structured logging, basic data persistence required for auth/session state.
- **De-scoped:** Legacy API (`EsgAsAService.Api`), V1/V2 dual domain, ad-hoc imports, AI narratives, miscellaneous endpoints outside report generation pipeline.

## Repository Resources Status
- `docs/ESG_datapunkter__B1-B11__C1-C9_.csv` present; sanity counts match requirements (B1:13 … C9:1).
- `docs/esg-formulas.md` contains partial definitions; several sections require formalised formulas/assumptions that will be codified per module with inline references.
- `README.md`, `TODO.md` outdated; will be rewritten after refactor.
- No `data/factors` directory yet — to be introduced with versioned JSON bundles.

## Target Architecture (Clean + Functional Core)
```
Domain
  ├─ Value objects: EsgInput, Section, Field, RequirementRule, FactorKey, FactorSelection
  ├─ Records: ModuleResult, ResultEnvelope, Assumptions, Warnings
  └─ Service interfaces: IModuleCalculator (pure), IFactorResolver contract types
Application
  ├─ Schema: CsvSchemaParser, InputSchemaBuilder, InputSchemaValidator
  ├─ Validation: FluentValidation DTO validators, coverage guard, enum generation
  ├─ Calculations: ModuleCalculator (invokes RunB1…RunC9 static functions), ResultComposer
  ├─ Mapping: FormulaMapBuilder (links datapoint → module/formula source)
  └─ Orchestration: ReportPipeline (coordinates schema validation, calculations, pdf request)
Infrastructure
  ├─ Factors: JsonFactorRepository (auto), CustomFactorOverrideStore, logging adapters
  ├─ Schema: File-backed schema cache, JSON schema emitter (`/schema/input.schema.json`), coverage CLI
  ├─ PDF: QuestPdfReportRenderer (deterministic layout, metadata, attachments)
  ├─ Logging: CorrelationId middleware, structured factor trace sink
  └─ Persistence: EF Core/Identity context isolated in AuthDbContext (only for auth/session)
Web (ASP.NET Core 9, Razor Components + minimal APIs)
  ├─ Wizard UI: Schema-driven steps, localization (da-DK resx), accessibility helpers, autosave via local storage
  ├─ API: `POST /generateReport` returning { pdfBytes, results } with correlation-id header; only accessible with [Authorize(Roles="SustainabilityLead")]
  └─ Infrastructure shell: Auth boundary, factor upload UI for custom overrides, export/import JSON endpoints
Tests
  ├─ Domain/Application unit tests (≥90% coverage) — deterministic module functions using sample fixtures
  ├─ Integration tests: RunAllModules orchestration, PDF snapshot (byte-for-byte) using baseline fixture, contract test for `/generateReport`
  └─ Tooling tests: Schema coverage check, factor repository loading, wizard component rendering smoke (bUnit)
```

## Implementation Phases
1. **CSV → Schema foundation**
   - Implement `CsvSchemaParser` in Application layer to produce `EsgInputSchema` (sections + fields).
   - Emit `schema/input.schema.json` & `schema/formula_map.json` via Infrastructure service.
   - Add `tools/SchemaCoverage` console app invoked in CI; fails if counts or mappings drift.
   - Introduce `scripts/check-schema-coverage.sh` (wraps dotnet tool) and wire into GitHub Actions.

2. **Domain & Calculation Core**
   - Define domain records for Input/Fields/Factors/Results (immutable, using `NodaTime` for dates).
   - Create `ModuleCalculations` static class with `RunB1`…`RunC9` methods returning `ModuleResult` (ValueRaw, Unit, Method, Sources[], Trace, ValueRounded).
   - Encode formulas from `docs/esg-formulas.md`; where absent, derive formulas and document references in XML comments + README method appendix.
   - Build `RunAllModules` orchestrator performing validation, dependency resolution, and fail-fast on missing inputs (no null/NaN).
   - Implement assumption tracking (`Assumptions` record) injected into calculations for transparency.

3. **Factor Management**
   - Create `/data/factors/{version}/auto.json` with metadata (name, source, unit, region, validFrom/To).
   - Implement `JsonFactorRepository` (Infrastructure) + `IFactorResolver` (Application) to merge auto + custom (override by `(name, unit)`).
   - Log selections + conversions into `ModuleResult.Trace` and structured logs.
   - Provide custom factor upload UI/API limited to authorized roles; persisted outside domain (e.g. JSON file in app data or DB table isolated from domain models).

4. **Wizard Rebuild**
   - Replace `EsgWizard.razor` with schema-driven component set:
     - Step generator groups CSV sections by module (B1…C9) with dynamic field rendering (supports `boolean`, `integer`, `number`, `string`, `array`, `object`).
     - `depends_on` interprets JSONPath-like references; uses state evaluation for visibility.
     - Client+server validation via FluentValidation (DTO) + generated JSON schema; surfaces requirement-based errors.
     - Autosave to `localStorage` via JS interop; manual import/export of full JSON snapshot.
     - Accessibility: labelled controls, ARIA roles for wizard navigation, keyboard traps avoided.
     - Localization: resources stored under `Resources/Wizard.da.resx`; default culture set to `da-DK`.

5. **PDF Generator Overhaul**
   - Implement deterministic QuestPDF document (fixed fonts, explicit layout) storing metadata: correlation-id, schema version, factor version.
   - Include sections: cover, methodology (including assumptions + rounding rules), data quality, module pages with tables (value/unit/method/factors), appendix with trace logs.
   - Ensure reproducibility by sorting inputs, disabling timestamps except ISO dates explicitly provided.
   - Generate sample artifacts: `/output/input.example.json`, `/output/results.example.json`, `/output/report.example.pdf`.

6. **Endpoints & Shell**
   - Remove `EsgAsAService.Api` project; expose minimal endpoints within Web project:
     - `POST /generateReport` (authorized) — accepts validated input JSON, returns `application/zip` (contains PDF + results) or multipart? (decision: respond with JSON containing base64 PDF & results object).
     - `GET /schema` returns current schema + formula map (read-only, authorized).
   - Maintain Identity endpoints and 2FA flows via Razor/Scaffold; isolate auth DbContext from domain (no shared entities).
   - Introduce CorrelationId middleware and logging scope per request.

7. **Quality Gates & Tooling**
   - Enable analyzers + StyleCop; treat warnings as errors via `Directory.Build.props` update.
   - Add `.editorconfig` root with formatting, naming rules (records PascalCase, etc.).
   - Update GitHub Actions workflow: build, lint, `dotnet test` with coverage threshold (coverlet + ReportGenerator), run schema coverage.
   - Provide `test-report.html` artifact (coverage + test summary).

8. **Documentation & Migration**
   - Rewrite `README.md` (architecture overview, setup, wizard usage, factor management, testing).
   - Refresh `TODO.md` to reflect new backlog (accessibility audits, future modules, etc.).
   - Add `docs/migrations-2025.md` capturing removed features (API, legacy modules) and reasoning.
   - Document calculation formulas + assumptions inline + aggregated in README appendix.

## Key Design Decisions
- **Functional Core:** Module calculations implemented as pure functions returning deterministic `ModuleResult`. Shell handles IO, validation, logging.
- **Validation Strategy:** CSV `requirement` produces rule set (`required`, `conditionally_required`, `optional`); enforced client+server. Missing coverage fails build via coverage tool.
- **Depends On Syntax:** adopt simple dotted path (e.g. `B3.scope1_kg`). Parser resolves at runtime to evaluate visibility.
- **Rounding Policy:** Central helper `RoundingRules.Round(value, precision)` applied at PDF render; `ValueRaw` preserved.
- **Factor Traceability:** `ModuleResult.Trace` includes structured JSON (stringified) capturing factor keys, conversion chains, assumptions.
- **Localization:** Danish default, but resource-driven for extensibility; ensures wizard + PDF share text via resource service.
- **Testing:** Use deterministic sample dataset stored in `tests/Fixtures`; PDF snapshot hashed to ensure byte equality.
- **Performance:** Preload schema/factors into memory, reuse compiled QuestPDF document where possible. Target <3s end-to-end with sample data.

## Outstanding Questions & Follow-Ups
- Storage for custom factors (file vs DB). Initial plan: JSON stored in `App_Data/custom-factors.json`; can be swapped for DB provider later.
- Authentication integration for wizard import/export endpoints — align with existing roles (default to SustainabilityLead/DataSteward).
- Accessibility verification pipeline (axe tests) — to be added after base refactor if time permits.

## Next Steps
1. Implement schema parser + emit artifacts (`schema/` directory).
2. Scaffold new domain records + calculation skeletons with unit tests per module.
3. Introduce factor repository + sample data set.
4. Rebuild wizard UI and `/generateReport` endpoint.
5. Overhaul PDF renderer + produce deterministic fixtures.
6. Update CI/tests/docs and generate demo artifacts.

