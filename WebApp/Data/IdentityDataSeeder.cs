using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Data
{
    // Creates the application's Identity roles and upgrades accounts created
    // before role support was introduced to the default Volunteer role.
    public static class IdentityDataSeeder
    {
        public static async Task SeedAsync(
            IServiceProvider services,
            string? bootstrapAdministratorEmail = null)
        {
            await using AsyncServiceScope scope = services.CreateAsyncScope();
            RoleManager<IdentityRole> roleManager =
                scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            UserManager<ApplicationUser> userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            ILoggerFactory loggerFactory =
                scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            ILogger logger = loggerFactory.CreateLogger(typeof(IdentityDataSeeder));

            int createdRoleCount = await CreateMissingRolesAsync(roleManager);
            int updatedUserCount = await AssignDefaultRoleToExistingUsersAsync(userManager);
            bool administratorBootstrapped =
                await BootstrapAdministratorAsync(
                    userManager,
                    bootstrapAdministratorEmail,
                    logger);

            logger.LogInformation(
                "Identity role setup completed. Created {CreatedRoleCount} roles, assigned Volunteer to {UpdatedUserCount} existing users, and bootstrapped an administrator: {AdministratorBootstrapped}.",
                createdRoleCount,
                updatedUserCount,
                administratorBootstrapped);
        }

        // Role creation is safe to run every time the application starts.
        private static async Task<int> CreateMissingRolesAsync(
            RoleManager<IdentityRole> roleManager)
        {
            int createdRoleCount = 0;

            foreach (string roleName in AppRoles.All)
            {
                if (await roleManager.RoleExistsAsync(roleName))
                {
                    continue;
                }

                IdentityResult result =
                    await roleManager.CreateAsync(new IdentityRole(roleName));

                if (!result.Succeeded)
                {
                    throw CreateIdentityException(
                        $"Unable to create the '{roleName}' role.",
                        result);
                }

                createdRoleCount++;
            }

            return createdRoleCount;
        }

        // Existing users with an assigned role keep their current access.
        // Accounts without a role receive the safe public-registration default.
        private static async Task<int> AssignDefaultRoleToExistingUsersAsync(
            UserManager<ApplicationUser> userManager)
        {
            int updatedUserCount = 0;
            List<ApplicationUser> users = await userManager.Users.ToListAsync();

            foreach (ApplicationUser user in users)
            {
                IList<string> currentRoles = await userManager.GetRolesAsync(user);
                if (currentRoles.Count > 0)
                {
                    continue;
                }

                IdentityResult result =
                    await userManager.AddToRoleAsync(user, AppRoles.Volunteer);

                if (!result.Succeeded)
                {
                    throw CreateIdentityException(
                        $"Unable to assign the Volunteer role to user '{user.Id}'.",
                        result);
                }

                updatedUserCount++;
            }

            return updatedUserCount;
        }

        // A configured account becomes the first administrator only while no
        // organisation administrator exists. The email belongs in user secrets
        // or deployment configuration, never in source-controlled settings.
        private static async Task<bool> BootstrapAdministratorAsync(
            UserManager<ApplicationUser> userManager,
            string? administratorEmail,
            ILogger logger)
        {
            IList<ApplicationUser> existingAdministrators =
                await userManager.GetUsersInRoleAsync(
                    AppRoles.OrganisationAdministrator);
            if (existingAdministrators.Count > 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(administratorEmail))
            {
                logger.LogWarning(
                    "No organisation administrator exists. Configure Identity:BootstrapAdministratorEmail to promote the first administrator.");
                return false;
            }

            ApplicationUser? user =
                await userManager.FindByEmailAsync(administratorEmail.Trim());
            if (user is null)
            {
                logger.LogWarning(
                    "The configured bootstrap administrator account does not exist yet.");
                return false;
            }

            IList<string> currentRoles = await userManager.GetRolesAsync(user);
            IdentityResult addResult =
                await userManager.AddToRoleAsync(
                    user,
                    AppRoles.OrganisationAdministrator);
            if (!addResult.Succeeded)
            {
                throw CreateIdentityException(
                    "Unable to assign the bootstrap administrator role.",
                    addResult);
            }

            string[] rolesToRemove = currentRoles
                .Where(role => role != AppRoles.OrganisationAdministrator)
                .ToArray();
            if (rolesToRemove.Length > 0)
            {
                IdentityResult removeResult =
                    await userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    await userManager.RemoveFromRoleAsync(
                        user,
                        AppRoles.OrganisationAdministrator);
                    throw CreateIdentityException(
                        "Unable to replace the bootstrap administrator's previous role.",
                        removeResult);
                }
            }

            IdentityResult stampResult =
                await userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
            {
                throw CreateIdentityException(
                    "Unable to invalidate the bootstrap administrator's existing sessions.",
                    stampResult);
            }

            return true;
        }

        private static InvalidOperationException CreateIdentityException(
            string message,
            IdentityResult result)
        {
            string errors = string.Join(
                "; ",
                result.Errors.Select(error => error.Description));

            return new InvalidOperationException($"{message} {errors}");
        }
    }
}
