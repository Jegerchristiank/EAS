using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using EsgAsAService.Infrastructure.Data;
using System.Text;
using System.Text.Json;

namespace EsgAsAService.Tests.Integration;

internal record IdResponse(Guid Id);

public class ApiIntegrationTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;
    public ApiIntegrationTests(WebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_Returns200()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Reports_GenerateJson_FeatureDisabled_Returns404()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsync($"/v1/reports/generate/json?periodId={Guid.NewGuid():D}", new StringContent(""));
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // Note: enabled JSON report flow is covered by controller/unit tests. Integration test can be added later.

    // Additional integration flows (approvals/process) can be added when needed.

    private static StringContent JsonContent(object o)
        => new StringContent(JsonSerializer.Serialize(o), Encoding.UTF8, "application/json");

    private static async Task<T> DeserializeAsync<T>(HttpResponseMessage res)
    {
        var s = await res.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(s, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    private static async Task<T> DeserializeAnonymousAsync<T>(HttpResponseMessage res, T _) =>
        JsonSerializer.Deserialize<T>(await res.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    private static Guid ExtractIdFromLocation(HttpResponseMessage res)
    {
        var loc = res.Headers.Location?.ToString() ?? string.Empty;
        var last = loc.TrimEnd('/').Split('/').LastOrDefault();
        return Guid.TryParse(last, out var id) ? id : Guid.Empty;
    }

    [Fact]
    public async Task Water_Invoice_RequiresCsv_Returns201_OnValid()
    {
        var client = _factory.CreateClient();
        using var mp = new MultipartFormDataContent();
        mp.Add(new StringContent(Guid.NewGuid().ToString()), "organisationId");
        var bytes = System.Text.Encoding.UTF8.GetBytes("intake_m3\n1\n");
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        mp.Add(file, "file", "water.csv");
        var res = await client.PostAsync("/v1/imports/water/invoice", mp);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.Contains("/v1/imports/water/process/", res.Headers.Location!.ToString());
    }
}

public sealed class WebAppFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace authentication with permissive test scheme (role=Admin)
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            // Replace DbContext with InMemory provider for fast tests
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<EsgDbContext>) ||
                d.ServiceType == typeof(EsgDbContext) ||
                (d.ServiceType.FullName?.Contains("DbContextPool") ?? false) ||
                (d.ServiceType.FullName?.Contains("ScopedDbContextLease") ?? false) ||
                (d.ServiceType.FullName?.Contains("EsgDbContext") ?? false)
            ).ToList();
            foreach (var d in toRemove) services.Remove(d);
            services.AddDbContext<EsgDbContext>(o => o.UseInMemoryDatabase($"esg-tests-{Guid.NewGuid():N}"));
        });
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((ctx, cfg) =>
        {
            var tempDb = Path.Combine(Path.GetTempPath(), $"esg-tests-{Guid.NewGuid():N}.db");
            try { using var _ = File.Create(tempDb); } catch { }
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"DataSource={tempDb};Cache=Shared",
                ["FeatureManagement:FullEsgJson"] = "false",
                ["DisableStartupSql"] = "true"
            }!);
        });
        return base.CreateHost(builder);
    }
}

file sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "tester"),
            new Claim(ClaimTypes.Role, EsgAsAService.Api.Auth.Rbac.Admin)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
