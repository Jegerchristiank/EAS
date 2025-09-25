using System.Reflection;
using EsgAsAService.Api.Auth;
using EsgAsAService.Api.Filters;
using EsgAsAService.Api.Services;
using EsgAsAService.Application.Abstractions;
using EsgAsAService.Application.Calculations;
using EsgAsAService.Infrastructure.Factors;
using EsgAsAService.Infrastructure.Data;
using EsgAsAService.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Swashbuckle.AspNetCore.Filters;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using EsgAsAService.Application.Schema;
using EsgAsAService.Infrastructure.Schema;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    // Global API v1 prefix and consistent error handling
    options.Conventions.Insert(0, new EsgAsAService.Api.Infrastructure.RoutePrefixConvention("v1"));
    options.Filters.Add<ApiExceptionFilter>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xml);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});
builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<EsgAsAService.Application.Abstractions.IDiagnosticService, EsgAsAService.Infrastructure.Diagnostics.DiagnosticService>();
builder.Services.AddFeatureManagement();
builder.Services.Configure<EsgSchemaOptions>(options =>
{
    options.CsvPath = builder.Configuration["EsgSchema:CsvPath"];
});
builder.Services.AddSingleton<CsvSchemaParser>();
builder.Services.AddSingleton<IEsgInputSchemaProvider, FileSystemEsgInputSchemaProvider>();
builder.Services.Configure<FactorRepositoryOptions>(options =>
{
    options.RootPath = builder.Configuration["Factors:RootPath"] ?? options.RootPath;
});
builder.Services.AddSingleton<IFactorRepository, JsonFactorRepository>();
builder.Services.AddSingleton(ModuleFunctions.CreateDefaultRegistry());
builder.Services.AddScoped<IEsgCalculationEngine, ModuleCalculationEngine>();

// OpenTelemetry (basic wiring)
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName: "EsgAsAService.Api"))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation();
        t.AddHttpClientInstrumentation();
        t.AddEntityFrameworkCoreInstrumentation();
        var otlp = builder.Configuration["OTLP_ENDPOINT"] ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        if (!string.IsNullOrWhiteSpace(otlp))
        {
            t.AddOtlpExporter();
        }
        else if (builder.Environment.IsDevelopment())
        {
            t.AddConsoleExporter();
        }
    })
    .WithMetrics(m =>
    {
        m.AddAspNetCoreInstrumentation();
        m.AddHttpClientInstrumentation();
        if (builder.Environment.IsDevelopment()) m.AddConsoleExporter();
    });

// CORS (tight in prod, permissive in dev)
var corsPolicy = "Default";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Allow requests from any localhost origin in development to support dynamic dev ports
            policy.SetIsOriginAllowed(origin =>
                    Uri.TryCreate(origin, UriKind.Absolute, out var o) &&
                    (o.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || o.Host == "127.0.0.1"))
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            policy.WithOrigins(builder.Configuration["Cors:AllowedOrigin"] ?? "https://example.com")
                  .WithMethods("GET","POST","PUT","PATCH")
                  .WithHeaders("Content-Type","Authorization");
        }
    });
});

var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? "DataSource=../EsgAsAService.Web/Data/app.db;Cache=Shared";
// Why: DbContext pooling reduces allocation/initialization overhead under load.
builder.Services.AddDbContextPool<EsgDbContext>(o => o.UseSqlite(conn));

builder.Services
    .AddIdentityCore<ApplicationUser>(o => o.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<EsgDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(o =>
{
    o.DefaultScheme = IdentityConstants.ApplicationScheme;
    o.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddIdentityCookies();

builder.Services.AddAuthorization(options =>
{
    Rbac.ConfigurePolicies(options);
});

// Application services
builder.Services.AddScoped<IUnitConversionService, UnitConversionService>();
builder.Services.AddScoped<ICsvImportService, CsvImportService>();
builder.Services.AddScoped<ICalculationRunner, CalculationRunner>();
builder.Services.AddScoped<IVsmeReportService, VsmeBasicReportService>();
builder.Services.AddScoped<IEsgFullReportService, EsgFullReportService>();
builder.Services.AddScoped<IEmissionFactorCache, EmissionFactorCache>();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    // Default global limiter
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.User?.Identity?.Name ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // Tighter policy for ingestion endpoints to protect backend processing
    options.AddPolicy("ingest", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ctx.User?.Identity?.Name ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30, // 30 req/min per user/IP for ingest operations
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

// Health checks (DB readiness)
// Why: surface a simple readiness endpoint for k8s/compose and for ops to verify DB connectivity.
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<EsgDbContext>("db");

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors(corsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

// Health endpoints
app.MapHealthChecks("/health");

// Ensure DB is migrated at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EsgDbContext>();
    var disableStartupSql = app.Configuration.GetValue<bool>("DisableStartupSql");
    if (!disableStartupSql)
    {
        // Harden startup with retries to allow DB to come up
        var attempts = 0; Exception? last = null;
        while (attempts < 5)
        {
            try
            {
                await db.Database.MigrateAsync();
                last = null; break;
            }
            catch (Exception ex)
            {
                last = ex; attempts++; await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempts)));
            }
        }
        if (last is not null) throw last;

        await db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS ""AuditLogs"" (
        ""Id"" TEXT NOT NULL CONSTRAINT ""PK_AuditLogs"" PRIMARY KEY,
        ""Timestamp"" TEXT NOT NULL,
        ""UserId"" TEXT NULL,
        ""EntityName"" TEXT NOT NULL,
        ""EntityId"" TEXT NOT NULL,
        ""Action"" TEXT NOT NULL,
        ""PayloadHash"" TEXT NOT NULL,
        ""PayloadJson"" TEXT NULL
    );");
        // Dev: create lightweight KPI views (SQLite)
        await db.Database.ExecuteSqlRawAsync(@"CREATE VIEW IF NOT EXISTS emissions_s1s2 AS
        SELECT act.ReportingPeriodId AS period_id,
               SUM(CASE WHEN se.Scope=1 THEN cr.Co2eKg ELSE 0 END) AS scope1_kg,
               SUM(CASE WHEN se.Scope=2 THEN cr.Co2eKg ELSE 0 END) AS scope2_kg,
               SUM(CASE WHEN se.Scope=3 THEN cr.Co2eKg ELSE 0 END) AS scope3_kg
        FROM CalculationResults cr
        JOIN ScopeEntries se ON se.Id = cr.ScopeEntryId
        JOIN Activities act ON act.Id = se.ActivityId
        GROUP BY act.ReportingPeriodId;");
        await db.Database.ExecuteSqlRawAsync(@"CREATE VIEW IF NOT EXISTS emissions_intensity AS
        SELECT e.period_id,
               (e.scope1_kg + e.scope2_kg + e.scope3_kg) AS total_kg,
               f.Revenue AS revenue,
               CASE WHEN IFNULL(f.Revenue,0)=0 THEN NULL ELSE (e.scope1_kg + e.scope2_kg + e.scope3_kg)/f.Revenue END AS intensity_kg_per_revenue
        FROM emissions_s1s2 e LEFT JOIN Financials f ON f.ReportingPeriodId = e.period_id;");
        await db.Database.ExecuteSqlRawAsync(@"CREATE VIEW IF NOT EXISTS water_consumption AS
        SELECT wm.ReportingPeriodId AS period_id,
               SUM(IFNULL(wm.IntakeM3,0) - IFNULL(wm.DischargeM3,0)) AS consumption_m3
        FROM WaterMeters wm GROUP BY wm.ReportingPeriodId;");
        await db.Database.ExecuteSqlRawAsync(@"CREATE VIEW IF NOT EXISTS accident_frequency AS
        SELECT si.ReportingPeriodId AS period_id,
               SUM(CAST(si.IncidentsCount AS REAL)) AS incidents_count,
               SUM(IFNULL(si.HoursWorked,0)) AS hours_worked,
               CASE WHEN SUM(IFNULL(si.HoursWorked,0))=0 THEN NULL ELSE (SUM(CAST(si.IncidentsCount AS REAL))/SUM(IFNULL(si.HoursWorked,0)))*200000 END AS afr
        FROM SafetyIncidents si GROUP BY si.ReportingPeriodId;");

        // Indexes for frequent lookups (SQLite)
        await db.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS IX_Activities_ReportingPeriodId ON Activities(ReportingPeriodId);");
        await db.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS IX_ScopeEntries_ActivityId ON ScopeEntries(ActivityId);");
        await db.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS IX_CalculationResults_ScopeEntryId ON CalculationResults(ScopeEntryId);");
        await db.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS IX_WaterMeters_ReportingPeriodId ON WaterMeters(ReportingPeriodId);");
        await db.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS IX_WasteManifests_ReportingPeriodId ON WasteManifests(ReportingPeriodId);");
        await db.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS IX_MaterialFlows_ReportingPeriodId ON MaterialFlows(ReportingPeriodId);");
    }
    else
    {
        // Lightweight schema for tests
        await db.Database.EnsureCreatedAsync();
    }
    // Prewarm emission factor cache (best-effort): common country/year/type combos
    try
    {
        var cache = scope.ServiceProvider.GetRequiredService<IEmissionFactorCache>();
        var combos = db.EmissionFactorsV2.AsNoTracking()
            .Select(f => new { f.Country, f.Year, f.Type })
            .Distinct()
            .Take(100)
            .ToList();
        foreach (var c in combos)
        {
            await cache.GetValueAsync(c.Country, c.Year, c.Type);
        }
    }
    catch { /* non-fatal */ }
}

await app.RunAsync();

// For WebApplicationFactory integration testing
public partial class Program { }
