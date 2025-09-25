Migrations (EF Core)

Commands (run from repo root):

1) Add migration for extended ESG entities

  DOTNET_ROLL_FORWARD=Major \
  dotnet ef migrations add AddEsgExtendedEntities \
    --project src/EsgAsAService.Infrastructure \
    --startup-project src/EsgAsAService.Api \
    --output-dir Migrations

2) Update database (dev SQLite)

  DOTNET_ROLL_FORWARD=Major \
  dotnet ef database update \
    --project src/EsgAsAService.Infrastructure \
    --startup-project src/EsgAsAService.Api

Tips
- SDK 9+ kræves. `Microsoft.EntityFrameworkCore.Design` er refereret i Infrastructure.
- I dev: API kalder `Database.Migrate()` ved opstart og opretter/udvider schema automatisk.
- Prod: Peg `DefaultConnection` på mål-RDBMS og kør samme EF-kommandoer i CI/CD eller ved deployment.
