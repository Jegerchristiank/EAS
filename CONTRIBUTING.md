# Contributing

## Prerequisites
- .NET 8/9 SDK
- SQLite (dev) / PostgreSQL (prod)

## Workflow
- Branch from `main`
- Small, atomic PRs with passing CI (build, analyzers, tests)
- Write/Update tests for all changes

## Coding Standards
- Nullable enabled, analyzers on (see Directory.Build.props)
- Keep services DI-registered; no service locators
- Controllers return DTOs, not domain entities (avoid leaking versioning fields)

## Engineering Principles (short)
- Write code others can read: meaningful names, consistent style, small functions.
- Prefer small, frequent commits with clear messages.
- Cover new functionality with tests; run the test suite before pushing.
- Remove duplication (DRY) without over‑abstracting.
- Document the why; the what should be obvious from the code.
- Stick to established patterns unless there’s a strong reason to deviate.
- Refactor continuously to keep debt low; keep deps minimal and updated.
- Make builds reproducible and runnable locally; CI is not optional.
- Make errors/logs human‑understandable; surface diagnostic codes where useful.

## Testing
- Unit tests: `dotnet test tests/EsgAsAService.Tests/EsgAsAService.Tests.csproj`
- E2E: add Playwright specs under `tests/E2E`
