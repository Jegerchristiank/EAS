using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EsgAsAService.Infrastructure.Identity;

public static class IdentitySeeder
{
    public const string AdminRole = "Administrator";
    public const string UserRole = "User";

    private static readonly Action<ILogger, string, string, Exception?> _roleCreateFailed =
        LoggerMessage.Define<string, string>(LogLevel.Warning, new EventId(1001, nameof(_roleCreateFailed)), "Failed creating role {Role}: {Errors}");

    private static readonly Action<ILogger, string, Exception?> _seededAdminUser =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1002, nameof(_seededAdminUser)), "Seeded admin user {Email}");

    private static readonly Action<ILogger, string, Exception?> _assignedAdminRole =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1003, nameof(_assignedAdminRole)), "Assigned admin role to user {Email}");

    private static readonly Action<ILogger, string, Exception?> _assignAdminRoleFailed =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1004, nameof(_assignAdminRoleFailed)), "Assigning admin role failed: {Errors}");

    private static readonly Action<ILogger, string, Exception?> _adminUserCreationFailed =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1005, nameof(_adminUserCreationFailed)), "Admin user creation failed: {Errors}");

    public static async Task SeedAsync(IServiceProvider sp, ILogger logger)
    {
        using var scope = sp.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Ensure roles
        var roles = new[] { AdminRole, UserRole, "SustainabilityLead", "DataSteward", "Auditor" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var res = await roleManager.CreateAsync(new IdentityRole(role));
                if (!res.Succeeded)
                {
                    var errors = string.Join(", ", res.Errors.Select(e => e.Description));
                    _roleCreateFailed(logger, role, errors, null);
                }
            }
        }

        // Optional admin user from env vars
        var adminEmail = Environment.GetEnvironmentVariable("ESG_ADMIN_EMAIL");
        var adminPwd = Environment.GetEnvironmentVariable("ESG_ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPwd))
        {
            return;
        }

        var user = await userManager.FindByEmailAsync(adminEmail);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            var createRes = await userManager.CreateAsync(user, adminPwd);
            if (!createRes.Succeeded)
            {
                var errors = string.Join(", ", createRes.Errors.Select(e => e.Description));
                _adminUserCreationFailed(logger, errors, null);
                return;
            }
            _seededAdminUser(logger, adminEmail!, null);
        }

        if (!await userManager.IsInRoleAsync(user, AdminRole))
        {
            var addRoleRes = await userManager.AddToRoleAsync(user, AdminRole);
            if (addRoleRes.Succeeded)
            {
                _assignedAdminRole(logger, adminEmail!, null);
            }
            else
            {
                var errors = string.Join(", ", addRoleRes.Errors.Select(e => e.Description));
                _assignAdminRoleFailed(logger, errors, null);
            }
        }
    }
}
