// Purpose:
// Verifies that Feature 5 shifts, notifications and conflict checks use the
// database rather than disappearing with an in-memory service instance.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Volunteer_Management_System;
using WebApp.Data;
using WebApp.Services.Scheduling;

namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class VolunteerSchedulePersistenceServiceTests
    {
        // A second service instance can read the shift saved by the first,
        // representing a WebApp restart against the same database.
        [TestMethod]
        public async Task RecordAssignmentAsync_NewServiceInstanceRetainsSchedule()
        {
            TestDbContextFactory factory = CreateFactory();
            VolunteerSchedulePersistenceService firstService = new(factory);
            AssignmentDetails details = CreateAssignmentDetails();

            await firstService.RecordAssignmentAsync(
                details.Assignment,
                details.Opportunity);

            VolunteerSchedulePersistenceService restartedService = new(factory);
            IReadOnlyList<VolunteerScheduledShiftModel> schedule =
                await restartedService.GetScheduleAsync(
                    details.Assignment.VolunteerId.ToString());

            Assert.HasCount(1, schedule);
            Assert.AreEqual(details.Assignment.Id, schedule[0].AssignmentId);
            Assert.AreEqual(details.Opportunity.Title, schedule[0].Title);
            Assert.AreEqual(details.Opportunity.Location, schedule[0].Location);
        }

        // Cancelling an assignment removes it from the active schedule while
        // preserving both assignment and cancellation notifications.
        [TestMethod]
        public async Task RecordCancellationAsync_RemovesShiftAndAddsNotification()
        {
            TestDbContextFactory factory = CreateFactory();
            VolunteerSchedulePersistenceService service = new(factory);
            AssignmentDetails details = CreateAssignmentDetails();
            string volunteerId = details.Assignment.VolunteerId.ToString();
            await service.RecordAssignmentAsync(
                details.Assignment,
                details.Opportunity);

            details.Assignment.Cancel();
            await service.RecordCancellationAsync(
                details.Assignment,
                details.Opportunity);

            Assert.HasCount(0, await service.GetScheduleAsync(volunteerId));
            IReadOnlyList<VolunteerShiftNotificationModel> notifications =
                await service.GetNotificationsAsync(volunteerId);
            Assert.HasCount(2, notifications);
            Assert.IsTrue(notifications.Any(notification =>
                notification.Type ==
                    VolunteerShiftNotificationType.Assigned));
            Assert.IsTrue(notifications.Any(notification =>
                notification.Type ==
                    VolunteerShiftNotificationType.Cancelled));
        }

        // Read state is saved in the database and remains read when queried
        // through a new persistence service instance.
        [TestMethod]
        public async Task MarkAllNotificationsAsReadAsync_PersistsReadState()
        {
            TestDbContextFactory factory = CreateFactory();
            VolunteerSchedulePersistenceService service = new(factory);
            AssignmentDetails details = CreateAssignmentDetails();
            string volunteerId = details.Assignment.VolunteerId.ToString();
            await service.RecordAssignmentAsync(
                details.Assignment,
                details.Opportunity);

            await service.MarkAllNotificationsAsReadAsync(volunteerId);

            VolunteerSchedulePersistenceService restartedService = new(factory);
            Assert.AreEqual(
                0,
                await restartedService
                    .GetUnreadNotificationCountAsync(volunteerId));
        }

        // Persisted shifts participate in overlap checks after the original
        // in-memory assignment service is no longer available.
        [TestMethod]
        public async Task HasConflictAsync_AfterServiceRestartDetectsOverlap()
        {
            TestDbContextFactory factory = CreateFactory();
            VolunteerSchedulePersistenceService service = new(factory);
            AssignmentDetails details = CreateAssignmentDetails();
            string volunteerId = details.Assignment.VolunteerId.ToString();
            await service.RecordAssignmentAsync(
                details.Assignment,
                details.Opportunity);

            VolunteerSchedulePersistenceService restartedService = new(factory);
            bool overlapping = await restartedService.HasConflictAsync(
                volunteerId,
                details.Opportunity.StartDateTime.AddMinutes(30),
                details.Opportunity.EndDateTime.AddHours(1));
            bool nonOverlapping = await restartedService.HasConflictAsync(
                volunteerId,
                details.Opportunity.EndDateTime.AddMinutes(1),
                details.Opportunity.EndDateTime.AddHours(2));

            Assert.IsTrue(overlapping);
            Assert.IsFalse(nonOverlapping);
        }

        private static AssignmentDetails CreateAssignmentDetails()
        {
            DateTime start = DateTime.Now.AddDays(7);
            VolunteerOpportunity opportunity = VolunteerOpportunity.Create(
                "Beach Cleanup",
                "Help clean the beach.",
                "Mission Bay",
                start,
                start.AddHours(3),
                "Teamwork",
                5);
            VolunteerAssignment assignment = VolunteerAssignment.Create(
                Guid.NewGuid(),
                opportunity.Id,
                Guid.NewGuid(),
                Guid.NewGuid());

            return new AssignmentDetails(assignment, opportunity);
        }

        private static TestDbContextFactory CreateFactory()
        {
            InMemoryDatabaseRoot root = new();
            DbContextOptions<ApplicationDbContext> options =
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString(), root)
                    .Options;
            return new TestDbContextFactory(options);
        }

        private sealed class TestDbContextFactory(
            DbContextOptions<ApplicationDbContext> options)
            : IDbContextFactory<ApplicationDbContext>
        {
            public ApplicationDbContext CreateDbContext() => new(options);

            public Task<ApplicationDbContext> CreateDbContextAsync(
                CancellationToken cancellationToken = default) =>
                Task.FromResult(CreateDbContext());
        }

        private sealed record AssignmentDetails(
            VolunteerAssignment Assignment,
            VolunteerOpportunity Opportunity);
    }
}
