using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WebApp.Data;
using WebApp.Services.Management;
using WebApp.Services.Scheduling;

namespace Volunteer_Management_System.Tests
{
    // Checks that the Features 2 and 3 records can be read by a new service
    // instance, which represents the WebApp starting again.
    [TestClass]
    public class VolunteerManagementPersistenceServiceTests
    {
        [TestMethod]
        public async Task CreatedOpportunityAndApplication_AreAvailableAfterNewServiceInstance()
        {
            TestDbContextFactory factory = CreateFactory();
            VolunteerAvailabilityService availability = new();
            VolunteerManagementPersistenceService first = CreateService(factory, availability);
            DateTime start = DateTime.UtcNow.AddDays(3);

            VolunteerOpportunity opportunity = await first.CreateOpportunityAsync(
                "Community garden", "Prepare the garden beds.", "Auckland",
                start, start.AddHours(2), "Gardening", 2);
            await first.PublishOpportunityAsync(opportunity.Id);
            User volunteer = User.Create(Guid.NewGuid(), "volunteer", "volunteer@example.com", Role.Volunteer);
            await first.SubmitApplicationAsync(volunteer, opportunity.Id);

            // A fresh service has no in-memory state from the first one.
            VolunteerManagementPersistenceService restarted = CreateService(factory, availability);
            IReadOnlyList<VolunteerOpportunity> opportunities = await restarted.GetOpportunitiesAsync(false);
            IReadOnlyList<VolunteerApplication> applications = await restarted.GetApplicationsForVolunteerAsync(volunteer.Id);

            Assert.HasCount(1, opportunities);
            Assert.AreEqual("Community garden", opportunities[0].Title);
            Assert.HasCount(1, applications);
            Assert.AreEqual(opportunity.Id, applications[0].OpportunityId);
            Assert.AreEqual(VolunteerApplicationStatus.Pending, applications[0].Status);
        }

        private static VolunteerManagementPersistenceService CreateService(
            TestDbContextFactory factory,
            VolunteerAvailabilityService availability) =>
            new(factory, availability, new VolunteerSchedulePersistenceService(factory));

        private static TestDbContextFactory CreateFactory()
        {
            InMemoryDatabaseRoot root = new();
            DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString(), root)
                .Options;
            return new TestDbContextFactory(options);
        }

        private sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
            : IDbContextFactory<ApplicationDbContext>
        {
            public ApplicationDbContext CreateDbContext() => new(options);
            public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(CreateDbContext());
        }
    }
}
