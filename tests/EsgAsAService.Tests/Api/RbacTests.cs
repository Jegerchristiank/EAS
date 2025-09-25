using EsgAsAService.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace EsgAsAService.Tests.Api;

public class RbacTests
{
    [Fact]
    public void ConfigurePolicies_DefinesExpectedPolicies()
    {
        var options = new AuthorizationOptions();
        Rbac.ConfigurePolicies(options);
        Assert.NotNull(options.GetPolicy("CanRead"));
        Assert.NotNull(options.GetPolicy("CanIngestData"));
        Assert.NotNull(options.GetPolicy("CanManageReferenceData"));
        Assert.NotNull(options.GetPolicy("CanCalculate"));
        Assert.NotNull(options.GetPolicy("CanApprove"));
    }
}

