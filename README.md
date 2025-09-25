ESG-as-a-Service (ESG reporting platform) software

Overview
- .NET 9 backend (ASP.NET Core, EF Core) with RBAC, audit, versioning
- Imports (CSV) via staging, med feltmapping og enheds-normalisering
- Emissionsfaktor-opslag (land/år/type, scope 1/2/3)
- ScopeEntry‑beregninger (deterministiske), evidens, godkendelser
- Rapporter: VSME basic, fuld ESG JSON (B1–B11, C1–C9) + grundlag for PDF/XBRL

What’s new
- Fuld ESG JSON-rapport generator (B1–B11, C‑placeholder) via service og endpoint.
- Controller-tests for JSON-rapport (501 når service mangler, 200 OK når registreret).
- Rydning af ubrugte filer (scaffold `WebApp1` fjernet), tilføjet `.gitignore`, og general repo‑oprydning.
- Centraliseret NuGet versionstyring via `Directory.Packages.props` + `Directory.Build.props`.
- Dev‑DB fil fjernet fra repo; DB oprettes/migreres ved opstart via EF `Migrate()`.

Quickstart (dev)
- Prereqs: .NET 9 SDK, SQLite (dev), Node(optional)
- Build: `DOTNET_ROLL_FORWARD=Major dotnet build -c Debug`
- Run API: `DOTNET_ROLL_FORWARD=Major ASPNETCORE_URLS=http://localhost:5198 dotnet run --project src/EsgAsAService.Api`
- Run Web: `DOTNET_ROLL_FORWARD=Major dotnet run --project src/EsgAsAService.Web --urls="http://localhost:5097;https://localhost:7036"`
- Tests: `DOTNET_ROLL_FORWARD=Major dotnet test tests/EsgAsAService.Tests/EsgAsAService.Tests.csproj -c Debug` (inkl. integrationstests med TestServer)
- Alt-i-én (build + test + start API & Web): `bash scripts/build-test-all.sh --run` (kræver en trusted dev‑cert: `dotnet dev-certs https --trust`)
  - Tilpas porte i `.env` via `WEB_PORT`, `WEB_PORT_HTTPS` og `API_PORT` hvis du ikke vil bruge defaults (5097/7036/5198)
- DB (dev): DB skema oprettes/udvides ved API‑opstart via `Database.Migrate()`. For nye tabeller:
  - Se `MIGRATIONS.md` og kør `dotnet ef migrations add AddEsgExtendedEntities` + `dotnet ef database update`.

Docker (lokal)
- Konfigurer `.env` i roden (eller brug default værdier):
  - `OPENAI_API_KEY` (valgfri) for AI‑narrativer i PDF
  - `ESG_ENABLE_AI_NARRATIVES=true` (valgfri) for at vise narrativer
  - `FEATURE_FULL_ESG_JSON=true` for at aktivere fuld JSON‑rapport
  - `WEB_PORT`, `API_PORT` for host‑porte
- Byg og kør: `docker compose up --build`
- Web: http://localhost:${WEB_PORT} (default 5097), API: http://localhost:${API_PORT} (default 5198)
- Data: SQLite for API gemmes i named volume `esg-data` (via `ConnectionStrings__DefaultConnection=DataSource=/data/app.db`).

AI (ChatGPT)
- Model: configured via `OpenAI:Model` (default now `gpt-5`).
- API key: set via env var `OPENAI_API_KEY` (recommended) eller via appsettings/User Secrets `OpenAI:ApiKey`.
- Base URL: defaults til `https://api.openai.com/v1` (Chat Completions).

Sådan sætter du API-nøglen (lokal udvikling):
- macOS/Linux (shell): `export OPENAI_API_KEY=sk-...`
- Windows (PowerShell): `$env:OPENAI_API_KEY = "sk-..."`
- Alternativt med User Secrets (ingen hemmeligheder i filsystemet):
  - `cd src/EsgAsAService.Web`
  - `dotnet user-secrets set "OpenAI:ApiKey" "sk-..."`

Relaterede filer
- Generator: `src/EsgAsAService.Infrastructure/AI/OpenAiTextGenerator.cs`
- Konfiguration: `src/EsgAsAService.Web/appsettings.json` (sektion `OpenAI`)

RBAC roles
- Admin, SustainabilityLead, DataSteward, Auditor (server-side authorization on all write endpoints)

Key Endpoints (API)
- Versioning prefix: all endpoints are served under `/v1/...`
- Organisations: GET/POST/PUT `/v1/organisations`
- Periods: GET/POST/PUT `/v1/reporting-periods`
- Locations: GET/POST `/v1/locations`
- Policies: GET/PUT `/v1/policies/{orgId}`; Certificates: POST/GET `/v1/certificates`
- Units + Conversions: GET/POST `/v1/units`, POST `/v1/units/conversions`
- Emission factors: GET `/v1/emission-factors`
- Energy readings: GET/POST `/v1/energy/readings`
- Water meters: GET/POST `/v1/water/meters`
- Waste manifests: GET/POST `/v1/waste/manifests`
- Materials flows: GET/POST `/v1/materials/flows`
- HR: GET/POST `/v1/hr/headcount`, `/v1/hr/payroll`, `/v1/hr/training`
- Safety incidents: GET/POST `/v1/safety/incidents`
- Pollution register: GET/POST `/v1/pollution`
- Governance cases: GET/POST `/v1/governance/cases`
- Evidence: POST `/v1/evidence/presign` → PUT upload
- Activities/ScopeEntries: GET/POST/PUT
- Calculations: POST `/v1/calculations/run?periodId=...`
- Approvals: POST `/v1/approvals/submit`, PATCH `/v1/approvals/{id}`
- Metrics: GET `/v1/metrics/emissions`, `/v1/metrics/water`, `/v1/metrics/accidents`, `/v1/metrics/gender-pay-gap`
- Reports: POST `/v1/reports/vsme/basic/generate`, `/v1/reports/export/xbrl`, `/v1/reports/generate/json?periodId=`
  - Bemærk: JSON endpoint returnerer 404, hvis angivet `periodId` ikke findes.
  - Feature flag: `/v1/reports/generate/json` kræver `FullEsgJson` aktiveret (FeatureManagement)
- Imports (staging): POST `/v1/imports/energy/invoice`, `/v1/imports/water/invoice`, `/v1/imports/waste/manifest`
  - Energy processing: POST `/v1/imports/energy/process/{docId}` (CSV kolonner: date, carrier, quantity, unit [, location])
  - Water processing: POST `/v1/imports/water/process/{docId}`
  - Invoice endpoints return 201 Created with Location til process‑ruten
    - CSV: enten `intake_m3 [, discharge_m3]` eller `intake,intake_unit [, discharge,discharge_unit]` (konverteres til m3)
  - Waste processing: POST `/imports/waste/process/{docId}`
    - CSV: enten `eak_code, quantity_kg, disposition` eller `eak_code, quantity, unit, disposition` (konverteres til kg)
  - Idempotens: Genkøring af `process` ignorerer allerede indlæste linjer (via normaliseret linje‑payload)

Full ESG JSON report
- Endpoint: `POST /reports/generate/json?periodId={GUID}`
- Returnerer sektionerne: B1 (grundlag), B2 (politikker), B3 (CO2e scope1/2/3 + intensitet), B4 (forurening), B5 (følsomme områder), B6 (vand), B7 (affald/materialer), B8 (arbejdsstyrke), B9 (arbejdsmiljø), B10 (løngab/overenskomst/uddannelse), B11 (virksomhedsadfærd). C1–C9 er p.t. placeholders.
- Eksempel (uddrag):
  `{ "B3": { "scope2_kg": 200, "total_kg": 200 }, "B6": { "consumption_m3": 8 } }`

Development Notes
- Audit + versioning logged for inserts/updates (payload hash + diffs)
- Paging on list endpoints with `page` + `pageSize`
- Rate limiting enabled (per user/IP); tighter `ingest` policy on import endpoints
- CORS (dev): tillader alle `localhost`/`127.0.0.1` origins
- Health checks: `GET /health` for readiness (inkl. DB check)
 - OpenTelemetry: tracing/metrics wired; Console exporter i dev, OTLP hvis `OTEL_EXPORTER_OTLP_ENDPOINT` er sat
 - Feature Flags: `Microsoft.FeatureManagement` registreret (brug `Features:*` i config til gradvis aktivering)
 
Imports – rationale
- Idempotens: Vi gemmer en normaliseret linje‑payload i `StagingLines` og skipper gentagelser pr. dokument samt på tværs af uploads (samme org/period/type), så re‑importer ikke laver dubletter.
- Enheder: CSV kan medbringe værdi + enhed; vi konverterer til kanoniske enheder (vand=m3, affald=kg) ved processering.
- Sikkerhed: simple MIME‑sniffing/indholdstjek ved upload (kræver tekst/csv‑lignende input); hårdere scanning kan tilføjes i prod.

CI
- GitHub Actions workflow kører build + tests ved push/PR (`.github/workflows/ci.yml`).

Next Steps (high‑impact)
- Consolidate V1/V2 domænemodeller eller planlæg migrations til én model.
- Standardiser API‑fejl med ProblemDetails + valideringsformat; tilføj API‑versionering.
- Feature flags for gradvis aktivering af nye imports/rapporter.
- OpenTelemetry: traces + metrics (ASP.NET Core, EF Core, HttpClient).
- Docker multi‑stage builds og compose til lokal end‑to‑end.

Docs
- Se `TODO.md` for fuld ESG-plan (B1–B11, C1–C9)
- Se `MIGRATIONS.md` for database migrations
- `API.md`, `CONTRIBUTING.md`, `SECURITY.md`, `CHANGELOG.md`

Housekeeping
- `.gitignore` ignorerer nu build‑artefakter, lokale DB‑filer, PID- og logfiler.
- Ubrugt scaffold‑projekt `WebApp1` er fjernet fra repoet.

Error Model (ProblemDetails)
- Validation errors return 400 with `ValidationProblemDetails` (extensions: `code=validation_error`).
- Known errors map to HTTP codes with diagnostic header:
  - 400: invalid argument/operation
  - 403: forbidden
  - 404: not found (e.g. `period_not_found`)
  - 500: internal error (header `X-Diagnostic-Code` for support)

Local Tips
- Ryd artefakter: `dotnet clean` eller slet `**/bin` og `**/obj` (genereres igen).
- Kør hurtige tests ofte: `dotnet test -c Debug --no-build`.
- Fejl/logs: fejlmeddelelser forsøger at forklare “hvorfor” (ikke kun “hvad”).

Feature Flags
- Aktiver fuld ESG JSON rapport: sæt i konfiguration (appsettings eller miljøvariabel):
  - appsettings.json: `{ "FeatureManagement": { "FullEsgJson": true } }`
  - env var: `FeatureManagement__FullEsgJson=true`
