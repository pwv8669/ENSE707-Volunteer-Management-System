using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Data;
using WebApp.Services.Identity;

namespace Volunteer_Management_System.Tests
{
    // Tests server-side authorization and safety rules for changing account roles.
    [TestClass]
    public class UserRoleManagementServiceTests
    {
        // Test that a Volunteer cannot bypass the administrator-only page and call the service directly.
        [TestMethod]
        public async Task UpdateRoleAsync_WithVolunteerActor_ThrowsUnauthorizedAccessException()
        {
            await using ServiceProvider services = CreateServices();
            await IdentityDataSeeder.SeedAsync(services);

            await using AsyncServiceScope scope = services.CreateAsyncScope();
            UserManager<ApplicationUser> userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            ApplicationUser actor = await CreateUserAsync(
                userManager,
                "volunteer@example.com",
                AppRoles.Volunteer);
            ApplicationUser target = await CreateUserAsync(
                userManager,
                "target@example.com",
                AppRoles.Volunteer);
            UserRoleManagementService service =
                new(userManager);

            await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() =>
                service.UpdateRoleAsync(
                    actor.Id,
                    target.Id,
                    AppRoles.Coordinator));
        }

        // Test that a Coordinator cannot bypass the administrator-only page and call the service directly.
        [TestMethod]
        public async Task UpdateRoleAsync_WithCoordinatorActor_ThrowsUnauthorizedAccessException()
        {
            await using ServiceProvider services = CreateServices();
            await IdentityDataSeeder.SeedAsync(services);

            await using AsyncServiceScope scope = services.CreateAsyncScope();
            UserManager<ApplicationUser> userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            ApplicationUser actor = await CreateUserAsync(
                userManager,
                "coordinator@example.com",
                AppRoles.Coordinator);
            ApplicationUser target = await CreateUserAsync(
                userManager,
                "target@example.com",
                AppRoles.Volunteer);
            UserRoleManagementService service =
                new(userManager);

            await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() =>
                service.UpdateRoleAsync(
                    actor.Id,
                    target.Id,
                    AppRoles.OrganisationAdministrator));
        }

        // Test that an administrator can replace a user's Volunteer role with Coordinator.
        [TestMethod]
        public async Task UpdateRoleAsync_WithAdministrator_ReplacesExistingRole()
        {
            await using ServiceProvider services = CreateServices();
            await IdentityDataSeeder.SeedAsync(services);

            await using AsyncServiceScope scope = services.CreateAsyncScope();
            UserManager<ApplicationUser> userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            ApplicationUser administrator = await CreateUserAsync(
                userManager,
                "administrator@example.com",
                AppRoles.OrganisationAdministrator);
            ApplicationUser target = await CreateUserAsync(
                userManager,
                "target@example.com",
                AppRoles.Volunteer);
            UserRoleManagementService service =
                new(userManager);

            RoleUpdateResult result = await service.UpdateRoleAsync(
                administrator.Id,
                target.Id,
                AppRoles.Coordinator);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(
                await userManager.IsInRoleAsync(target, AppRoles.Coordinator));
            Assert.IsFalse(
                await userManager.IsInRoleAsync(target, AppRoles.Volunteer));
        }

        // Test that the system cannot be left without an organisation administrator.
        [TestMethod]
        public async Task UpdateRoleAsync_WithFinalAdministrator_PreventsDemotion()
        {
            await using ServiceProvider services = CreateServices();
            await IdentityDataSeeder.SeedAsync(services);

            await using AsyncServiceScope scope = services.CreateAsyncScope();
            UserManager<ApplicationUser> userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            ApplicationUser administrator = await CreateUserAsync(
                userManager,
                "administrator@example.com",
                AppRoles.OrganisationAdministrator);
            UserRoleManagementService service =
                new(userManager);

            RoleUpdateResult result = await service.UpdateRoleAsync(
                administrator.Id,
                administrator.Id,
                AppRoles.Volunteer);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(
                await userManager.IsInRoleAsync(
                    administrator,
                    AppRoles.OrganisationAdministrator));
        }

        private static async Task<ApplicationUser> CreateUserAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string role)
        {
            ApplicationUser user = new()
            {
                UserName = email,
                Email = email
            };

            IdentityResult createResult = await userManager.CreateAsync(user);
            Assert.IsTrue(createResult.Succeeded);
            IdentityResult roleResult = await userManager.AddToRoleAsync(user, role);
            Assert.IsTrue(roleResult.Succeeded);
            return user;
        }

        // Creates an isolated Identity database for each authorization test.
        private static ServiceProvider CreateServices()
        {
            ServiceCollection services = new();
            InMemoryDatabaseRoot databaseRoot = new();
            string databaseName = Guid.NewGuid().ToString();

            services.AddLogging();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName, databaseRoot));
            services.AddIdentityCore<ApplicationUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            return services.BuildServiceProvider();
        }
    }
}
