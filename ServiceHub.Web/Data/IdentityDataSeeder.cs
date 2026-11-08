using Microsoft.AspNetCore.Identity;
using ServiceHub.Web.Models;

namespace ServiceHub.Web.Data;

public static class IdentityDataSeeder
{
    public static async Task SeedRolesAndAdminAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(IdentityDataSeeder));

        foreach (var roleName in AppRoles.All)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded && !await roleManager.RoleExistsAsync(roleName))
            {
                var errorCodes = string.Join(", ", result.Errors.Select(error => error.Code));
                logger.LogError("Could not create the {RoleName} role. Identity errors: {ErrorCodes}", roleName, errorCodes);
                throw new InvalidOperationException($"Could not create the {roleName} role.");
            }
        }

        await MigrateLegacyProviderRoleAsync(roleManager, userManager, logger, cancellationToken);

        var adminEmail = configuration["InitialAdmin:Email"];
        var adminPassword = configuration["InitialAdmin:Password"];
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning(
                "Initial Admin account seeding was skipped because InitialAdmin:Email or InitialAdmin:Password is not configured.");
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Administrator",
                EmailConfirmed = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(admin, adminPassword);
            if (!createResult.Succeeded)
            {
                admin = await userManager.FindByEmailAsync(adminEmail);
                if (admin is null)
                {
                    var errorCodes = string.Join(", ", createResult.Errors.Select(error => error.Code));
                    logger.LogError("Could not create the configured initial Admin account. Identity errors: {ErrorCodes}", errorCodes);
                    throw new InvalidOperationException("Could not create the configured initial Admin account.");
                }
            }
        }

        if (!await userManager.IsInRoleAsync(admin, AppRoles.Admin))
        {
            var addRoleResult = await userManager.AddToRoleAsync(admin, AppRoles.Admin);
            if (!addRoleResult.Succeeded && !await userManager.IsInRoleAsync(admin, AppRoles.Admin))
            {
                var errorCodes = string.Join(", ", addRoleResult.Errors.Select(error => error.Code));
                logger.LogError("Could not assign the configured initial Admin role. Identity errors: {ErrorCodes}", errorCodes);
                throw new InvalidOperationException("Could not assign the configured initial Admin role.");
            }
        }
    }

    private static async Task MigrateLegacyProviderRoleAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var legacyRole = await roleManager.FindByNameAsync(AppRoles.LegacyProvider);
        if (legacyRole is null)
        {
            return;
        }

        foreach (var user in await userManager.GetUsersInRoleAsync(AppRoles.LegacyProvider))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!await userManager.IsInRoleAsync(user, AppRoles.ServiceProvider))
            {
                var addResult = await userManager.AddToRoleAsync(user, AppRoles.ServiceProvider);
                if (!addResult.Succeeded && !await userManager.IsInRoleAsync(user, AppRoles.ServiceProvider))
                {
                    var errorCodes = string.Join(", ", addResult.Errors.Select(error => error.Code));
                    logger.LogError("Could not migrate a legacy Provider account. Identity errors: {ErrorCodes}", errorCodes);
                    throw new InvalidOperationException("Could not migrate a legacy Provider account.");
                }
            }

            var removeResult = await userManager.RemoveFromRoleAsync(user, AppRoles.LegacyProvider);
            if (!removeResult.Succeeded && await userManager.IsInRoleAsync(user, AppRoles.LegacyProvider))
            {
                var errorCodes = string.Join(", ", removeResult.Errors.Select(error => error.Code));
                logger.LogError("Could not remove a legacy Provider role assignment. Identity errors: {ErrorCodes}", errorCodes);
                throw new InvalidOperationException("Could not remove a legacy Provider role assignment.");
            }
        }

        var deleteResult = await roleManager.DeleteAsync(legacyRole);
        if (!deleteResult.Succeeded && await roleManager.RoleExistsAsync(AppRoles.LegacyProvider))
        {
            var errorCodes = string.Join(", ", deleteResult.Errors.Select(error => error.Code));
            logger.LogError("Could not remove the legacy Provider role. Identity errors: {ErrorCodes}", errorCodes);
            throw new InvalidOperationException("Could not remove the legacy Provider role.");
        }
    }
}
