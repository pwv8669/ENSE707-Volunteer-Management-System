using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Data;

namespace Volunteer_Management_System.Tests
{
    // Tests role creation and the safe upgrade of accounts created before roles existed.
    [TestClass]
    public class IdentityDataSeederTests
    {
        // Test that all supported roles are created and a legacy account becomes a Volunteer.
        [TestMethod]
        public async Task SeedAsync_WithUserWithoutRole_CreatesRolesAndAssignsVolunteer()
        {
            await using ServiceProvider services = CreateServices();
            string userId;

            await using (AsyncServiceScope setupScope = services.CreateAsyncScope())
            {
                UserManager<ApplicationUser> userManager =
                    setupScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                ApplicationUser user = new()
                {
                    UserName = "existing@example.com",
                    Email = "existing@example.com"
                };

                IdentityResult createResult = await userManager.CreateAsync(user);
                Assert.IsTrue(createResult.Succeeded);
                userId = user.Id;
            }

            await IdentityDataSeeder.SeedAsync(services);
            // A second run confirms that seeding does not create duplicate data.
            await IdentityDataSeeder.SeedAsync(services);

            await using AsyncServiceScope verificationScope = services.CreateAsyncScope();
            RoleManager<IdentityRole> roleManager =
                verificationScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            UserManager<ApplicationUser> verificationUserManager =
                verificationScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            ApplicationUser? storedUser =
                await verificationUserManager.FindByIdAsync(userId);

            Assert.IsNotNull(storedUser);
            Assert.AreEqual(AppRoles.All.Count, await roleManager.Roles.CountAsync());
            foreach (string roleName in AppRoles.All)
            {
                Assert.IsTrue(await roleManager.RoleExistsAsync(roleName));
            }

            IList<string> assignedRoles =
                await verificationUserManager.GetRolesAsync(storedUser);
            CollectionAssert.AreEqual(
                new[] { AppRoles.Volunteer },
                assignedRoles.ToArray());
        }

        // Test that backfilling does not overwrite a role that was already assigned.
        [TestMethod]
        public async Task SeedAsync_WithCoordinator_PreservesExistingRole()
        {
            await using ServiceProvider services = CreateServices();
            await IdentityDataSeeder.SeedAsync(services);
            string userId;

            await using (AsyncServiceScope setupScope = services.CreateAsyncScope())
            {
                UserManager<ApplicationUser> userManager =
                    setupScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                ApplicationUser user = new()
                {
                    UserName = "coordinator@example.com",
                    Email = "coordinator@example.com"
                };

                IdentityResult createResult = await userManager.CreateAsync(user);
                Assert.IsTrue(createResult.Succeeded);
                IdentityResult roleResult =
                    await userManager.AddToRoleAsync(user, AppRoles.Coordinator);
                Assert.IsTrue(roleResult.Succeeded);
                userId = user.Id;
            }

            await IdentityDataSeeder.SeedAsync(services);

            await using AsyncServiceScope verificationScope = services.CreateAsyncScope();
            UserManager<ApplicationUser> verificationUserManager =
                verificationScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            ApplicationUser? storedUser =
                await verificationUserManager.FindByIdAsync(userId);

            Assert.IsNotNull(storedUser);
            Assert.IsTrue(
                await verificationUserManager.IsInRoleAsync(
                    storedUser,
                    AppRoles.Coordinator));
            Assert.IsFalse(
                await verificationUserManager.IsInRoleAsync(
                    storedUser,
                    AppRoles.Volunteer));
        }

        // Test that configured bootstrapping promotes the first administrator and removes Volunteer.
        [TestMethod]
        public async Task SeedAsync_WithBootstrapEmail_PromotesFirstAdministrator()
        {
            await using ServiceProvider services = CreateServices();
            const string administratorEmail = "administrator@example.com";
            string userId;

            await using (AsyncServiceScope setupScope = services.CreateAsyncScope())
            {
                UserManager<ApplicationUser> userManager =
                    setupScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                ApplicationUser user = new()
                {
                    UserName = administratorEmail,
                    Email = administratorEmail
                };

                IdentityResult createResult = await userManager.CreateAsync(user);
                Assert.IsTrue(createResult.Succeeded);
                userId = user.Id;
            }

            await IdentityDataSeeder.SeedAsync(services, administratorEmail);

            await using AsyncServiceScope verificationScope = services.CreateAsyncScope();
            UserManager<ApplicationUser> verificationUserManager =
                verificationScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            ApplicationUser? storedUser =
                await verificationUserManager.FindByIdAsync(userId);

            Assert.IsNotNull(storedUser);
            Assert.IsTrue(
                await verificationUserManager.IsInRoleAsync(
                    storedUser,
                    AppRoles.OrganisationAdministrator));
            Assert.IsFalse(
                await verificationUserManager.IsInRoleAsync(
                    storedUser,
                    AppRoles.Volunteer));
        }

        // Creates an isolated Identity database for each test.
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
