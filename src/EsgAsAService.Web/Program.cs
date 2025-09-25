using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EsgAsAService.Web.Components;
using EsgAsAService.Web.Components.Account;
using EsgAsAService.Infrastructure.Data;
using EsgAsAService.Infrastructure.Identity;
using EsgAsAService.Application.Abstractions;
using EsgAsAService.Application.Services;
using EsgAsAService.Infrastructure.EmissionFactors;
using EsgAsAService.Infrastructure.Reporting;
using EsgAsAService.Infrastructure.AI;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using QuestPDF.Infrastructure;
using EsgAsAService.Infrastructure.Modules;
using EsgAsAService.Application.Calculations;
using EsgAsAService.Application.Schema;
using EsgAsAService.Infrastructure.Schema;
using EsgAsAService.Infrastructure.Factors;
using EsgAsAService.Web.Services;
using EsgAsAService.Web.Localization;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseStaticWebAssets();

// Configure QuestPDF license once at startup (required by 2023+ versions)
QuestPDF.Settings.License = LicenseType.Community;

// In development, gracefully fall back to a free localhost port if the requested one is busy
if (builder.Environment.IsDevelopment())
{
    var urls = builder.Configuration["urls"] ?? builder.Configuration["ASPNETCORE_URLS"];
    if (!string.IsNullOrWhiteSpace(urls) && !urls.Contains(';', StringComparison.Ordinal))
    {
        if (Uri.TryCreate(urls, UriKind.Absolute, out var uri)
            && string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || uri.Host == "127.0.0.1")
            && uri.Port > 0)
        {
            var preferred = uri.Port;
            var selected = GetAvailablePort(preferred);
            if (selected != preferred)
            {
                Console.WriteLine($"[dev] Port {preferred} is busy. Using {selected} instead.");
                // Only override when we actually have to switch ports to avoid Kestrel override warning
                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.ListenLocalhost(selected);
                });
            }
        }
    }
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpClient();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// In dev, show detailed Blazor Server circuit errors
if (builder.Environment.IsDevelopment())
{
    builder.Services.Configure<CircuitOptions>(o => o.DetailedErrors = true);
}

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<EsgDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<EsgDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Application & Infrastructure services
builder.Services.AddScoped<ICalculationService, CalculationService>();
builder.Services.AddTransient<StaticEmissionFactorProvider>();
builder.Services.AddHttpClient<KlimakompassetEmissionFactorProvider>();
builder.Services.AddHttpClient<EfragVsmeEmissionFactorProvider>();
builder.Services.AddScoped<IEmissionFactorProvider>(sp =>
    new CompositeEmissionFactorProvider(new IEmissionFactorProvider[]
    {
        sp.GetRequiredService<KlimakompassetEmissionFactorProvider>(),
        sp.GetRequiredService<EfragVsmeEmissionFactorProvider>(),
        sp.GetRequiredService<StaticEmissionFactorProvider>()
    }));
builder.Services.AddScoped<IReportGenerator, ReportGenerator>();
builder.Services.AddScoped<IFullReportComposer, FullReportComposer>();
builder.Services.AddHttpClient<OpenAiTextGenerator>();
builder.Services.AddScoped<IAITextGenerator, OpenAiTextGenerator>();
builder.Services.AddScoped<IEsgDataService, EsgDataService>();
builder.Services.AddSingleton<IDiagnosticService, EsgAsAService.Infrastructure.Diagnostics.DiagnosticService>();
builder.Services.AddSingleton<IModuleCatalog, CsvModuleCatalog>();
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
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<DirectoryService>();
builder.Services.AddScoped<DashboardDataService>();
builder.Services.AddScoped<WorklistService>();
builder.Services.AddScoped<DiagnosticsClient>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<LocalizationService>();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = new[] { new CultureInfo("da-DK"), new CultureInfo("en-US") };
    options.DefaultRequestCulture = new RequestCulture("da-DK");
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
});

// Options from configuration / environment
builder.Services.Configure<KlimakompassetOptions>(builder.Configuration.GetSection("Klimakompasset"));
builder.Services.Configure<EfragVsmeOptions>(builder.Configuration.GetSection("EfragVsme"));
builder.Services.Configure<OpenAiOptions>(opt =>
{
    opt.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? builder.Configuration["OpenAI:ApiKey"];
    opt.BaseUrl = builder.Configuration["OpenAI:BaseUrl"] ?? opt.BaseUrl;
    opt.Model = builder.Configuration["OpenAI:Model"] ?? opt.Model;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Apply migrations to ensure all tables (including AuditLogs) exist
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<EsgDbContext>();
    await db.Database.MigrateAsync();
    // In dev, if no migrations are defined and DB already exists, EnsureCreated may be a no‑op.
    // Force-create missing tables based on the model.
    try { await db.Database.EnsureCreatedAsync(); } catch { /* ignore */ }
    try
    {
        var creator = db.Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
        if (creator != null)
        {
            try { await creator.CreateTablesAsync(); } catch { /* tables may already exist */ }
        }
    }
    catch { /* non-fatal in dev */ }
    // Ensure AuditLogs exists (older dev DBs may miss it)
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
    // Ensure PeriodMappings exists (explicit V1<->V2 mapping without migrations in dev)
    await db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS ""PeriodMappings"" (
        ""V1PeriodId"" TEXT NOT NULL CONSTRAINT ""PK_PeriodMappings"" PRIMARY KEY,
        ""V2PeriodId"" TEXT NOT NULL,
        ""CreatedAt"" TEXT NOT NULL
    );");
    // Ensure PeriodMappings exists (explicit V1<->V2 mapping without migrations in dev)
    await db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS ""PeriodMappings"" (
        ""V1PeriodId"" TEXT NOT NULL CONSTRAINT ""PK_PeriodMappings"" PRIMARY KEY,
        ""V2PeriodId"" TEXT NOT NULL,
        ""CreatedAt"" TEXT NOT NULL
    );");
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    // Basic security headers for production
    app.Use(async (ctx, next) =>
    {
        ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        // CSP: adjust if you add external resources; kept conservative by default
        ctx.Response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self'";
        await next();
    });
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();
var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Seed roles and optional admin user
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");
    await IdentitySeeder.SeedAsync(scope.ServiceProvider, logger);
}

app.MapGet("/api/modules", async (IModuleCatalog catalog, CancellationToken ct) =>
{
    var modules = await catalog.GetModulesAsync(ct);
    return Results.Ok(modules);
}).RequireAuthorization();

// Minimal endpoints to download reports
app.MapGet("/api/report/pdf/{periodId:guid}", async (Guid periodId, IEsgDataService data, IFullReportComposer composer, IReportGenerator reports) =>
{
    var rp = await data.GetReportingPeriodAsync(periodId);
    if (rp is null || rp.Company is null) return Results.NotFound();
    // Compose full report (enrich with V2 data when possible), then render
    var full = await composer.ComposeAsync(rp.Company, rp);
    var bytes = await reports.GeneratePdfAsync(full);
    return Results.File(bytes, "application/pdf", $"ESG-{rp.Company.Name}-{rp.Year}.pdf");
}).RequireAuthorization();

app.MapGet("/api/report/xbrl/{periodId:guid}", async (Guid periodId, IEsgDataService data, IReportGenerator reports) =>
{
    var rp = await data.GetReportingPeriodAsync(periodId);
    if (rp is null || rp.Company is null) return Results.NotFound();
    var bytes = await reports.GenerateXbrlAsync(rp.Company, rp);
    return Results.File(bytes, "application/xml", $"ESG-{rp.Company.Name}-{rp.Year}.xbrl");
}).RequireAuthorization();

await app.RunAsync();

// Helpers
static int GetAvailablePort(int preferred)
{
    if (IsPortFree(preferred)) return preferred;
    // Create and start a listener only inside this scope so Dispose/Stop runs before we return
    using (var listener = new TcpListener(IPAddress.Loopback, 0))
    {
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        return port; // listener disposed after the using block ends
    }
}

static bool IsPortFree(int port)
{
    try
    {
        // Ensure the listener is disposed before returning
        using (var listener = new TcpListener(IPAddress.Loopback, port))
        {
            listener.Start();
            return true;
        }
    }
    catch (SocketException)
    {
        return false;
    }
}
