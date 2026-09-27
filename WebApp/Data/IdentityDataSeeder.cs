using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Data
{
    // Creates the application's Identity roles and upgrades accounts created
    // before role support was introduced to the default Volunteer role.
    public static class IdentityDataSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
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

            logger.LogInformation(
                "Identity role setup completed. Created {CreatedRoleCount} roles and assigned the Volunteer role to {UpdatedUserCount} existing users.",
                createdRoleCount,
                updatedUserCount);
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
