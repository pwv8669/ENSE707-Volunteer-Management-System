using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WebApp.Data;
using WebApp.Services.Activity;

// This class contains unit tests for the WebApp's VolunteerActivitySyncService, which rebuilds the requests used by the Reporting and Dashboard page
// and the profile history from the real volunteer applications and the saved shifts and attendance.
namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class VolunteerActivitySyncServiceTests
    {
        // Creates a pending application for a new volunteer and a new opportunity.
        private static VolunteerApplication PendingApplication() =>
            VolunteerApplication.Create(Guid.NewGuid(), Guid.NewGuid());

        // Creates an application that a coordinator has approved.
        private static VolunteerApplication ApprovedApplication()
        {
            VolunteerApplication application = PendingApplication();
            application.Approve(Guid.NewGuid());
            return application;
        }

        // Creates a saved shift for an application, with an optional attendance result.
        private static ShiftActivity ShiftFor(
            VolunteerApplication application,
            VolunteerShiftStatus status = VolunteerShiftStatus.Assigned,
            VolunteerAttendanceStatus? attendance = null,
            decimal hours = 0m) =>
            new(
                Guid.NewGuid(),
                application.VolunteerId,
                application.OpportunityId,
                application.Id,
                status,
                DateTime.UtcNow.AddDays(-2),
                attendance,
                hours);

        // Test that an application that is still waiting for review becomes a pending request that keeps its ids.
        [TestMethod]
        public void BuildRequests_WithPendingApplication_ReturnsPendingRequest()
        {
            VolunteerApplication application = PendingApplication();

            IReadOnlyList<VolunteerRequest> requests =
                VolunteerActivitySyncService.BuildRequests([application], []);

            Assert.HasCount(1, requests);
            Assert.AreEqual(application.Id, requests[0].Id);
            Assert.AreEqual(application.VolunteerId, requests[0].VolunteerId);
            Assert.AreEqual(application.OpportunityId, requests[0].OpportunityId);
            Assert.AreEqual(VolunteerRequestStatus.Pending, requests[0].Status);
            Assert.IsNull(requests[0].RespondedAt);
            Assert.AreEqual(0.0, requests[0].HoursLogged);
        }

        // Test that a rejected application becomes a declined request with no hours.
        [TestMethod]
        public void BuildRequests_WithRejectedApplication_ReturnsDeclinedRequest()
        {
            VolunteerApplication application = PendingApplication();
            application.Reject(Guid.NewGuid());

            IReadOnlyList<VolunteerRequest> requests =
                VolunteerActivitySyncService.BuildRequests([application], []);

            Assert.AreEqual(VolunteerRequestStatus.Declined, requests[0].Status);
            Assert.IsNotNull(requests[0].RespondedAt);
            Assert.AreEqual(0.0, requests[0].HoursLogged);
        }

        // Test that an approved application with no attendance yet becomes a fulfilled request with zero hours.
        [TestMethod]
        public void BuildRequests_WithApprovedApplicationAndNoShifts_ReturnsFulfilledWithZeroHours()
        {
            VolunteerApplication application = ApprovedApplication();

            IReadOnlyList<VolunteerRequest> requests =
                VolunteerActivitySyncService.BuildRequests([application], []);

            Assert.AreEqual(VolunteerRequestStatus.Fulfilled, requests[0].Status);
            Assert.AreEqual(0.0, requests[0].HoursLogged);
        }

        // Test that hours recorded as attended on the volunteer's shift become the request's hours.
        [TestMethod]
        public void BuildRequests_WithAttendedShift_UsesTheAttendedHours()
        {
            VolunteerApplication application = ApprovedApplication();
            ShiftActivity shift = ShiftFor(application, attendance: VolunteerAttendanceStatus.Attended, hours: 2.5m);

            IReadOnlyList<VolunteerRequest> requests =
                VolunteerActivitySyncService.BuildRequests([application], [shift]);

            Assert.AreEqual(VolunteerRequestStatus.Fulfilled, requests[0].Status);
            Assert.AreEqual(2.5, requests[0].HoursLogged);
        }

        // Test that an absence counts as zero hours.
        [TestMethod]
        public void BuildRequests_WithAbsentShift_CountsZeroHours()
        {
            VolunteerApplication application = ApprovedApplication();
            ShiftActivity shift = ShiftFor(application, attendance: VolunteerAttendanceStatus.Absent);

            IReadOnlyList<VolunteerRequest> requests =
                VolunteerActivitySyncService.BuildRequests([application], [shift]);

            Assert.AreEqual(VolunteerRequestStatus.Fulfilled, requests[0].Status);
            Assert.AreEqual(0.0, requests[0].HoursLogged);
        }

        // Test that a shift with no attendance recorded yet counts as zero hours.
        [TestMethod]
        public void BuildRequests_WithShiftNotYetMarked_CountsZeroHours()
        {
            VolunteerApplication application = ApprovedApplication();
            ShiftActivity shift = ShiftFor(application);

            IReadOnlyList<VolunteerRequest> requests =
                VolunteerActivitySyncService.BuildRequests([application], [shift]);

            Assert.AreEqual(0.0, requests[0].HoursLogged);
        }

        // Test that hours from several attended shifts for one application are added together (1.5 + 2.25 = 3.75).
        [TestMethod]
        public void BuildRequests_WithTwoAttendedShifts_AddsTheHours()
        {
            VolunteerApplication application = ApprovedApplication();
            ShiftActivity first = ShiftFor(application, attendance: VolunteerAttendanceStatus.Attended, hours: 1.5m);
            ShiftActivity second = ShiftFor(application, attendance: VolunteerAttendanceStatus.Attended, hours: 2.25m);

            IReadOnlyList<VolunteerRequest> requests =
                VolunteerActivitySyncService.BuildRequests([application], [first, second]);

            Assert.AreEqual(3.75, requests[0].HoursLogged);
        }

        // Test that an approved application whose only shift was cancelled is shown as declined, because the volunteer is no longer taking part.
        [TestMethod]
        public void BuildRequests_WhenEveryShiftWasCancelled_ReturnsDeclinedRequest()
        {
            VolunteerApplication application = ApprovedApplication();
            ShiftActivity shift = ShiftFor(application, status: VolunteerShiftStatus.Cancelled);

            IReadOnlyList<VolunteerRequest> requests =
                VolunteerActivitySyncService.BuildRequests([application], [shift]);

            Assert.AreEqual(VolunteerRequestStatus.Declined, requests[0].Status);
            Assert.AreEqual(0.0, requests[0].HoursLogged);
        }

        // Test that hours recorded on a cancelled shift are ignored when another shift for the same application is still active.
        [TestMethod]
        public void BuildRequests_WithCancelledAndActiveShift_IgnoresTheCancelledShiftsHours()
        {
            VolunteerApplication application = ApprovedApplication();
            ShiftActivity cancelled = ShiftFor(
                application, VolunteerShiftStatus.Cancelled, VolunteerAttendanceStatus.Attended, 5m);
            ShiftActivity active = ShiftFor(
                application, VolunteerShiftStatus.Assigned, VolunteerAttendanceStatus.Attended, 2m);

            IReadOnlyList<VolunteerRequest> requests =
                VolunteerActivitySyncService.BuildRequests([application], [cancelled, active]);

            Assert.AreEqual(VolunteerRequestStatus.Fulfilled, requests[0].Status);
            Assert.AreEqual(2.0, requests[0].HoursLogged);
        }

        // Test that shifts for one application never change another application's request.
        [TestMethod]
        public void BuildRequests_WithTwoApplications_KeepsEachOnesHoursSeparate()
        {
            VolunteerApplication first = ApprovedApplication();
            VolunteerApplication second = ApprovedApplication();
            ShiftActivity firstShift = ShiftFor(first, attendance: VolunteerAttendanceStatus.Attended, hours: 4m);

            IReadOnlyList<VolunteerRequest> requests =
                VolunteerActivitySyncService.BuildRequests([first, second], [firstShift]);

            Assert.HasCount(2, requests);
            Assert.AreEqual(4.0, requests.Single(r => r.Id == first.Id).HoursLogged);
            Assert.AreEqual(0.0, requests.Single(r => r.Id == second.Id).HoursLogged);
        }

        // Test that a saved shift whose application is gone (for example after the app restarted) still counts as a fulfilled request with its hours.
        [TestMethod]
        public void BuildRequests_WithShiftWhoseApplicationIsGone_ReturnsFulfilledRequestFromTheShift()
        {
            VolunteerApplication lostApplication = ApprovedApplication();
            ShiftActivity shift = ShiftFor(lostApplication, attendance: VolunteerAttendanceStatus.Attended, hours: 3m);

            IReadOnlyList<VolunteerRequest> requests =
                VolunteerActivitySyncService.BuildRequests([], [shift]);

            Assert.HasCount(1, requests);
            Assert.AreEqual(lostApplication.Id, requests[0].Id);
            Assert.AreEqual(shift.VolunteerId, requests[0].VolunteerId);
            Assert.AreEqual(shift.OpportunityId, requests[0].OpportunityId);
            Assert.AreEqual(VolunteerRequestStatus.Fulfilled, requests[0].Status);
            Assert.AreEqual(3.0, requests[0].HoursLogged);
        }

        // Test that a cancelled shift whose application is gone shows as a declined request.
        [TestMethod]
        public void BuildRequests_WithCancelledShiftWhoseApplicationIsGone_ReturnsDeclinedRequest()
        {
            VolunteerApplication lostApplication = ApprovedApplication();
            ShiftActivity shift = ShiftFor(lostApplication, status: VolunteerShiftStatus.Cancelled);

            IReadOnlyList<VolunteerRequest> requests =
                VolunteerActivitySyncService.BuildRequests([], [shift]);

            Assert.AreEqual(VolunteerRequestStatus.Declined, requests[0].Status);
        }

        // Test that two shifts that belong to the same missing application make one request, not two.
        [TestMethod]
        public void BuildRequests_WithTwoShiftsForOneMissingApplication_ReturnsOneRequest()
        {
            VolunteerApplication lostApplication = ApprovedApplication();
            ShiftActivity first = ShiftFor(lostApplication, attendance: VolunteerAttendanceStatus.Attended, hours: 1m);
            ShiftActivity second = ShiftFor(lostApplication, attendance: VolunteerAttendanceStatus.Attended, hours: 2m);

            IReadOnlyList<VolunteerRequest> requests =
                VolunteerActivitySyncService.BuildRequests([], [first, second]);

            Assert.HasCount(1, requests);
            Assert.AreEqual(3.0, requests[0].HoursLogged);
        }

        // Test that a shift with a missing id can't be matched to anything, so it is skipped instead of causing an error.
        [TestMethod]
        public void BuildRequests_WithShiftThatHasNoApplicationId_IgnoresIt()
        {
            ShiftActivity shift = new(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.Empty,
                VolunteerShiftStatus.Assigned, DateTime.UtcNow, null, 0m);

            IReadOnlyList<VolunteerRequest> requests =
                VolunteerActivitySyncService.BuildRequests([], [shift]);

            Assert.HasCount(0, requests);
        }

        // Test that with no applications and no shifts there are no requests.
        [TestMethod]
        public void BuildRequests_WithNoData_ReturnsEmptyList()
        {
            Assert.HasCount(0, VolunteerActivitySyncService.BuildRequests([], []));
        }

        // Test that missing lists are rejected with an ArgumentNullException.
        [TestMethod]
        public void BuildRequests_WithNullLists_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                VolunteerActivitySyncService.BuildRequests(null!, []));
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                VolunteerActivitySyncService.BuildRequests([], null!));
        }

        // Test that refreshing reads the saved shift and attendance from the database and stores the matching fulfilled request with its hours.
        [TestMethod]
        public async Task RefreshAsync_WithSavedAttendance_StoresFulfilledRequestWithHours()
        {
            TestDbContextFactory factory = CreateFactory();
            Scenario scenario = CreateScenario(factory);
            VolunteerApplication application = scenario.SubmitApproved();
            await scenario.SaveShiftAsync(application, attended: true, hours: 3.5m);

            await scenario.Sync.RefreshAsync();

            VolunteerRequest? request = scenario.Requests.FindRequestById(application.Id);
            Assert.IsNotNull(request);
            Assert.AreEqual(VolunteerRequestStatus.Fulfilled, request.Status);
            Assert.AreEqual(3.5, request.HoursLogged);
        }

        // Test that refreshing replaces whatever requests were stored before, so old demo data can't linger.
        [TestMethod]
        public async Task RefreshAsync_ReplacesEarlierRequests()
        {
            TestDbContextFactory factory = CreateFactory();
            Scenario scenario = CreateScenario(factory);
            scenario.Requests.SubmitRequest(Guid.NewGuid(), Guid.NewGuid());
            scenario.Requests.SubmitRequest(Guid.NewGuid(), Guid.NewGuid());

            await scenario.Sync.RefreshAsync();

            Assert.HasCount(0, scenario.Requests.GetAllRequests());
        }

        // Test that a volunteer id that isn't a Guid is skipped, rather than breaking the whole refresh.
        [TestMethod]
        public async Task RefreshAsync_WithShiftForNonGuidVolunteer_SkipsThatShift()
        {
            TestDbContextFactory factory = CreateFactory();
            Scenario scenario = CreateScenario(factory);
            await scenario.SaveRawShiftAsync(volunteerId: "not-a-guid");

            await scenario.Sync.RefreshAsync();

            Assert.HasCount(0, scenario.Requests.GetAllRequests());
        }

        // Test that refreshing again after attendance changes picks up the new hours.
        [TestMethod]
        public async Task RefreshAsync_AfterAttendanceChanges_ShowsTheNewHours()
        {
            TestDbContextFactory factory = CreateFactory();
            Scenario scenario = CreateScenario(factory);
            VolunteerApplication application = scenario.SubmitApproved();
            VolunteerShiftRecord shift = await scenario.SaveShiftAsync(application, attended: true, hours: 2m);
            await scenario.Sync.RefreshAsync();
            Assert.AreEqual(2.0, scenario.Requests.FindRequestById(application.Id)!.HoursLogged);

            await scenario.CorrectAttendanceAsync(shift.Id, 4m);
            await scenario.Sync.RefreshAsync();

            Assert.AreEqual(4.0, scenario.Requests.FindRequestById(application.Id)!.HoursLogged);
        }

        // Test that many refreshes at the same time don't fail or leave the wrong number of requests.
        [TestMethod]
        public async Task RefreshAsync_CalledAtTheSameTime_StaysConsistent()
        {
            TestDbContextFactory factory = CreateFactory();
            Scenario scenario = CreateScenario(factory);
            VolunteerApplication application = scenario.SubmitApproved();
            await scenario.SaveShiftAsync(application, attended: true, hours: 1m);

            await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => scenario.Sync.RefreshAsync()));

            Assert.HasCount(1, scenario.Requests.GetAllRequests());
        }

        // Test that the dashboard's event statistics reflect real applications: one fulfilled with hours and one still pending.
        [TestMethod]
        public async Task RefreshAsync_FeedsTheReportingStatistics()
        {
            TestDbContextFactory factory = CreateFactory();
            Scenario scenario = CreateScenario(factory);
            VolunteerApplication approved = scenario.SubmitApproved();
            await scenario.SaveShiftAsync(approved, attended: true, hours: 3m);
            scenario.SubmitPendingFor(scenario.OpportunityId);
            ReportingService reporting = new(scenario.Requests, scenario.Opportunities);

            await scenario.Sync.RefreshAsync();

            EventStatistics statistics = reporting.GetEventStatistics(scenario.OpportunityId);
            Assert.AreEqual(2, statistics.RequestsReceived);
            Assert.AreEqual(1, statistics.FulfilledRequests);
            Assert.AreEqual(1, statistics.PendingRequests);
            Assert.AreEqual(3.0, statistics.TotalHoursLogged);
            Assert.HasCount(1, reporting.GetPendingRequests());
            Assert.HasCount(1, reporting.GetFulfilledRequests());
        }

        // Test that a volunteer's profile history shows their real approved activity with the hours the coordinator recorded.
        [TestMethod]
        public async Task RefreshAsync_FeedsTheVolunteerHistory()
        {
            TestDbContextFactory factory = CreateFactory();
            Scenario scenario = CreateScenario(factory);
            VolunteerApplication application = scenario.SubmitApproved();
            await scenario.SaveShiftAsync(application, attended: true, hours: 2.5m);
            VolunteerHistoryService history = new(scenario.Requests, scenario.Opportunities);

            await scenario.Sync.RefreshAsync();

            IReadOnlyList<VolunteerHistoryEntry> entries = history.GetHistory(application.VolunteerId);
            Assert.HasCount(1, entries);
            Assert.AreEqual("Beach Cleanup", entries[0].EventTitle);
            Assert.AreEqual(VolunteerRequestStatus.Fulfilled, entries[0].Status);
            Assert.AreEqual(2.5, history.GetSummary(application.VolunteerId).TotalHours);
        }

        // Builds an isolated in-memory database for one test.
        private static TestDbContextFactory CreateFactory()
        {
            InMemoryDatabaseRoot root = new();
            DbContextOptions<ApplicationDbContext> options =
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString(), root)
                    .Options;
            return new TestDbContextFactory(options);
        }

        private static Scenario CreateScenario(TestDbContextFactory factory) => new(factory);

        private sealed class TestDbContextFactory(
            DbContextOptions<ApplicationDbContext> options)
            : IDbContextFactory<ApplicationDbContext>
        {
            public ApplicationDbContext CreateDbContext() => new(options);

            public Task<ApplicationDbContext> CreateDbContextAsync(
                CancellationToken cancellationToken = default) =>
                Task.FromResult(CreateDbContext());
        }

        // One published "Beach Cleanup" opportunity, the real domain services around it, and helpers to save shifts and attendance.
        private sealed class Scenario
        {
            private readonly TestDbContextFactory factory;

            public Scenario(TestDbContextFactory factory)
            {
                this.factory = factory;
                Opportunities = new VolunteerOpportunityService();
                Applications = new VolunteerApplicationService(Opportunities);
                Requests = new VolunteerRequestService();
                Sync = new VolunteerActivitySyncService(Applications, Requests, factory);

                DateTime start = DateTime.UtcNow.AddDays(7);
                VolunteerOpportunity opportunity = Opportunities.CreateOpportunity(
                    "Beach Cleanup", "Clean the beach.", "Mission Bay", start, start.AddHours(3), "Teamwork", 10);
                Opportunities.PublishOpportunity(opportunity.Id);
                OpportunityId = opportunity.Id;
            }

            public VolunteerOpportunityService Opportunities { get; }

            public VolunteerApplicationService Applications { get; }

            public VolunteerRequestService Requests { get; }

            public VolunteerActivitySyncService Sync { get; }

            public Guid OpportunityId { get; }

            // A new volunteer applies and a coordinator approves.
            public VolunteerApplication SubmitApproved()
            {
                VolunteerApplication application = SubmitPendingFor(OpportunityId);
                application.Approve(Guid.NewGuid());
                return application;
            }

            // A new volunteer applies and the application stays pending.
            public VolunteerApplication SubmitPendingFor(Guid opportunityId)
            {
                string name = "volunteer" + Guid.NewGuid().ToString("N")[..6];
                User volunteer = User.Create(Guid.NewGuid(), name, name + "@example.com", Role.Volunteer);
                return Applications.SubmitApplication(volunteer, opportunityId);
            }

            // Saves the volunteer's shift for the application, and optionally an attendance result for it.
            public async Task<VolunteerShiftRecord> SaveShiftAsync(
                VolunteerApplication application,
                bool attended,
                decimal hours)
            {
                VolunteerShiftRecord shift = NewShift(application.VolunteerId.ToString(), application.OpportunityId, application.Id);

                await using ApplicationDbContext db = factory.CreateDbContext();
                db.VolunteerShifts.Add(shift);
                db.VolunteerAttendanceRecords.Add(new VolunteerAttendanceRecord
                {
                    ShiftId = shift.Id,
                    VolunteerId = shift.VolunteerId,
                    Status = attended ? VolunteerAttendanceStatus.Attended : VolunteerAttendanceStatus.Absent,
                    HoursCompleted = hours,
                    Notes = string.Empty,
                    RecordedByUserId = Guid.NewGuid().ToString(),
                    RecordedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
                return shift;
            }

            // Saves a shift with the given volunteer id text and no attendance.
            public async Task SaveRawShiftAsync(string volunteerId)
            {
                await using ApplicationDbContext db = factory.CreateDbContext();
                db.VolunteerShifts.Add(NewShift(volunteerId, Guid.NewGuid(), Guid.NewGuid()));
                await db.SaveChangesAsync();
            }

            // Changes the hours recorded for a shift, like a coordinator correcting attendance.
            public async Task CorrectAttendanceAsync(Guid shiftId, decimal hours)
            {
                await using ApplicationDbContext db = factory.CreateDbContext();
                VolunteerAttendanceRecord record = await db.VolunteerAttendanceRecords.SingleAsync(r => r.ShiftId == shiftId);
                record.HoursCompleted = hours;
                await db.SaveChangesAsync();
            }

            private static VolunteerShiftRecord NewShift(string volunteerId, Guid opportunityId, Guid applicationId)
            {
                DateTime startsAt = DateTime.Now.AddDays(-1);
                return new VolunteerShiftRecord
                {
                    Id = Guid.NewGuid(),
                    VolunteerId = volunteerId,
                    OpportunityId = opportunityId,
                    ApplicationId = applicationId,
                    AssignedByUserId = Guid.NewGuid(),
                    Title = "Beach Cleanup",
                    Location = "Mission Bay",
                    RequiredSkills = "Teamwork",
                    StartsAt = startsAt,
                    EndsAt = startsAt.AddHours(3),
                    AssignedAt = DateTime.UtcNow.AddDays(-2),
                    Status = VolunteerShiftStatus.Assigned
                };
            }
        }
    }
}
