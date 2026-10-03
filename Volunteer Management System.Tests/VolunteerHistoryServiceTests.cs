using System.Linq;

// This class contains unit tests for the VolunteerHistoryService class in the Volunteer Management System.
namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class VolunteerHistoryServiceTests
    {
        // Helper that creates a sample opportunity starting the given number of days from now.
        private static VolunteerOpportunity CreateOpportunity(
            VolunteerOpportunityService opportunityService,
            string title,
            int daysFromNow)
        {
            DateTime start = DateTime.UtcNow.AddDays(daysFromNow);

            return opportunityService.CreateOpportunity(
                title,
                "Sample description.",
                "Auckland",
                start,
                start.AddHours(3),
                "Teamwork",
                5);
        }

        // Test that creating the service without a request service throws an ArgumentNullException.
        [TestMethod]
        public void Constructor_WithNullRequestService_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new VolunteerHistoryService(null!, new VolunteerOpportunityService()));
        }

        // Test that creating the service without an opportunity service throws an ArgumentNullException.
        [TestMethod]
        public void Constructor_WithNullOpportunityService_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new VolunteerHistoryService(new VolunteerRequestService(), null!));
        }

        // Test that asking for history with an empty volunteer id throws an ArgumentException.
        [TestMethod]
        public void GetHistory_WithEmptyVolunteerId_ThrowsArgumentException()
        {
            VolunteerHistoryService service = new(
                new VolunteerRequestService(),
                new VolunteerOpportunityService());

            Assert.ThrowsExactly<ArgumentException>(() => service.GetHistory(Guid.Empty));
        }

        // Test that a volunteer with no requests has an empty history and zero totals.
        [TestMethod]
        public void GetHistory_WithNoRequests_ReturnsEmptyHistoryAndZeroSummary()
        {
            VolunteerHistoryService service = new(
                new VolunteerRequestService(),
                new VolunteerOpportunityService());
            Guid volunteerId = Guid.NewGuid();

            Assert.HasCount(0, service.GetHistory(volunteerId));

            VolunteerHistorySummary summary = service.GetSummary(volunteerId);
            Assert.AreEqual(0, summary.TotalActivities);
            Assert.AreEqual(0.0, summary.TotalHours);
        }

        // Test that each history entry carries the event's details and the request's status and hours.
        [TestMethod]
        public void GetHistory_WithFulfilledRequest_JoinsEventDetails()
        {
            VolunteerRequestService requestService = new();
            VolunteerOpportunityService opportunityService = new();
            VolunteerHistoryService service = new(requestService, opportunityService);
            Guid volunteerId = Guid.NewGuid();
            VolunteerOpportunity beachCleanup = CreateOpportunity(opportunityService, "Beach Cleanup", 3);

            VolunteerRequest request = requestService.SubmitRequest(volunteerId, beachCleanup.Id);
            requestService.FulfillRequest(request.Id);
            requestService.LogHours(request.Id, 2.5);

            VolunteerHistoryEntry entry = service.GetHistory(volunteerId).Single();

            Assert.AreEqual(request.Id, entry.RequestId);
            Assert.AreEqual("Beach Cleanup", entry.EventTitle);
            Assert.AreEqual("Auckland", entry.Location);
            Assert.AreEqual(beachCleanup.StartDateTime, entry.EventStart);
            Assert.AreEqual(beachCleanup.EndDateTime, entry.EventEnd);
            Assert.AreEqual(VolunteerRequestStatus.Fulfilled, entry.Status);
            Assert.AreEqual(2.5, entry.HoursLogged);
            Assert.IsNotNull(entry.RespondedAt);
        }

        // Test that history only includes the requested volunteer's activities.
        [TestMethod]
        public void GetHistory_WithSeveralVolunteers_ReturnsOnlyThatVolunteersRequests()
        {
            VolunteerRequestService requestService = new();
            VolunteerOpportunityService opportunityService = new();
            VolunteerHistoryService service = new(requestService, opportunityService);
            Guid volunteerId = Guid.NewGuid();
            VolunteerOpportunity opportunity = CreateOpportunity(opportunityService, "Food Bank", 2);

            requestService.SubmitRequest(volunteerId, opportunity.Id);
            requestService.SubmitRequest(Guid.NewGuid(), opportunity.Id);

            Assert.HasCount(1, service.GetHistory(volunteerId));
        }

        // Test that history is ordered with the latest event first.
        [TestMethod]
        public void GetHistory_WithSeveralEvents_OrdersLatestEventFirst()
        {
            VolunteerRequestService requestService = new();
            VolunteerOpportunityService opportunityService = new();
            VolunteerHistoryService service = new(requestService, opportunityService);
            Guid volunteerId = Guid.NewGuid();

            VolunteerOpportunity soon = CreateOpportunity(opportunityService, "Soon", 1);
            VolunteerOpportunity later = CreateOpportunity(opportunityService, "Later", 10);
            requestService.SubmitRequest(volunteerId, soon.Id);
            requestService.SubmitRequest(volunteerId, later.Id);

            CollectionAssert.AreEqual(
                new[] { "Later", "Soon" },
                service.GetHistory(volunteerId).Select(entry => entry.EventTitle).ToArray());
        }

        // Test that a request whose event was deleted still appears, with a placeholder title.
        [TestMethod]
        public void GetHistory_WhenEventWasDeleted_UsesPlaceholderTitle()
        {
            VolunteerRequestService requestService = new();
            VolunteerOpportunityService opportunityService = new();
            VolunteerHistoryService service = new(requestService, opportunityService);
            Guid volunteerId = Guid.NewGuid();
            VolunteerOpportunity opportunity = CreateOpportunity(opportunityService, "Cancelled Event", 4);

            requestService.SubmitRequest(volunteerId, opportunity.Id);
            opportunityService.DeleteOpportunity(opportunity.Id);

            VolunteerHistoryEntry entry = service.GetHistory(volunteerId).Single();

            Assert.AreEqual(VolunteerHistoryService.MissingEventTitle, entry.EventTitle);
            Assert.IsNull(entry.EventStart);
        }

        // Test that the summary counts each status and adds up the hours.
        [TestMethod]
        public void GetSummary_WithMixedStatuses_CountsEachStatusAndTotalsHours()
        {
            VolunteerRequestService requestService = new();
            VolunteerOpportunityService opportunityService = new();
            VolunteerHistoryService service = new(requestService, opportunityService);
            Guid volunteerId = Guid.NewGuid();
            VolunteerOpportunity opportunity = CreateOpportunity(opportunityService, "Garden", 5);

            VolunteerRequest first = requestService.SubmitRequest(volunteerId, opportunity.Id);
            requestService.FulfillRequest(first.Id);
            requestService.LogHours(first.Id, 3);

            VolunteerRequest second = requestService.SubmitRequest(volunteerId, opportunity.Id);
            requestService.FulfillRequest(second.Id);
            requestService.LogHours(second.Id, 1.5);

            VolunteerRequest declined = requestService.SubmitRequest(volunteerId, opportunity.Id);
            requestService.DeclineRequest(declined.Id);

            requestService.SubmitRequest(volunteerId, opportunity.Id);

            VolunteerHistorySummary summary = service.GetSummary(volunteerId);

            Assert.AreEqual(4, summary.TotalActivities);
            Assert.AreEqual(2, summary.FulfilledActivities);
            Assert.AreEqual(1, summary.PendingActivities);
            Assert.AreEqual(1, summary.DeclinedActivities);
            Assert.AreEqual(4.5, summary.TotalHours);
        }
    }
}
