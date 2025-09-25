# Changelog

## [Unreleased]
- Remove demo widgets (Counter/Weather)
- RBAC on Web minimal endpoints
- Typed VSME Basic model + XBRL export
- CSV import conversion enforcement
- Audit logs include update diffs
- Paging, rate limiting, CORS
- Added docs and CI workflow
 - Full ESG JSON report service (B1–B11 baseline + C placeholders)
 - Repo cleanup: removed unused `WebApp1` scaffold, added `.gitignore`, deleted placeholder classes
 - Cleanup: removed build artifacts (bin/obj) from workspace
 - Comments: added XML-docs to core interfaces and clarified controller “why” notes
- Docs: updated README with Next Steps; expanded TODO and CONTRIBUTING (engineering principles)
- Validation: CSV import now requires non-empty category field
 - API: Introduced `/v1` route prefix via convention (no controller changes)
- Errors: Unified ProblemDetails responses with `X-Diagnostic-Code` header
- CI: Removed duplicate `.github/workflows/dotnet.yml` (use `ci.yml`)
 - Infra: Switched to `AddDbContextPool` for EF Core
 - Observability: Added OpenTelemetry (ASP.NET/EF/Http) with Console/OTLP exporters
 - Feature flags: Introduced `Microsoft.FeatureManagement` for gradual rollout
 - Docker: Added multi-stage Dockerfiles for API/Web and `docker-compose.yml`
  - Rate limiting: Added per-endpoint `ingest` policy for import routes
  - Web security: Added CSP, X-Content-Type-Options, Referrer-Policy in production
 - DB dev indexes: Create common lookup indexes on SQLite at startup
  - Tests: Added initial integration tests via WebApplicationFactory (health, reports feature gate, water invoice)
