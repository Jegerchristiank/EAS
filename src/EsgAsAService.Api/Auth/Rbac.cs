using Microsoft.AspNetCore.Authorization;

namespace EsgAsAService.Api.Auth;

internal static class Rbac
{
    public const string Admin = "Admin";
    public const string SustainabilityLead = "SustainabilityLead";
    public const string DataSteward = "DataSteward";
    public const string Auditor = "Auditor";

    public static void ConfigurePolicies(AuthorizationOptions options)
    {
        options.AddPolicy("CanManageReferenceData", p => p.RequireRole(Admin));
        options.AddPolicy("CanIngestData", p => p.RequireRole(Admin, DataSteward));
        options.AddPolicy("CanCalculate", p => p.RequireRole(Admin, SustainabilityLead, DataSteward));
        options.AddPolicy("CanApprove", p => p.RequireRole(Admin, Auditor));
        options.AddPolicy("CanRead", p => p.RequireRole(Admin, SustainabilityLead, DataSteward, Auditor));
    }
}
