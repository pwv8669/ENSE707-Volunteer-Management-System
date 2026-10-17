using WebApp.Services;

// This class contains unit tests for the WebApp's DevelopmentSampleData class, which fills the in-memory services with demo data for local development.
namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class DevelopmentSampleDataTests
    {
        // Test that seeding an empty service creates four published opportunities that all start in the future.
        [TestMethod]
        public void SeedOpportunities_WithEmptyService_CreatesFourPublishedFutureOpportunities()
        {
            VolunteerOpportunityService opportunities = new();

            DevelopmentSampleData.SeedOpportunities(opportunities);

            IReadOnlyList<VolunteerOpportunity> seeded = opportunities.GetAllOpportunities();
            Assert.HasCount(4, seeded);
            foreach (VolunteerOpportunity opportunity in seeded)
            {
                Assert.AreEqual(OpportunityStatus.Published, opportunity.Status);
                Assert.IsTrue(opportunity.StartDateTime > DateTime.UtcNow);
            }
        }

        // Test that seeding twice does not add the same opportunities again.
        [TestMethod]
        public void SeedOpportunities_CalledTwice_DoesNotAddDuplicates()
        {
            VolunteerOpportunityService opportunities = new();

            DevelopmentSampleData.SeedOpportunities(opportunities);
            DevelopmentSampleData.SeedOpportunities(opportunities);

            Assert.HasCount(4, opportunities.GetAllOpportunities());
        }

        // Test that seeding leaves the data alone when opportunities already exist.
        [TestMethod]
        public void SeedOpportunities_WhenOpportunitiesAlreadyExist_LeavesThemAlone()
        {
            VolunteerOpportunityService opportunities = new();
            DateTime start = DateTime.UtcNow.AddDays(7);
            opportunities.CreateOpportunity(
                "Existing Event", "Already here.", "Auckland", start, start.AddHours(2), "Teamwork", 5);

            DevelopmentSampleData.SeedOpportunities(opportunities);

            Assert.HasCount(1, opportunities.GetAllOpportunities());
        }

        // Test that seeding with no service throws an ArgumentNullException.
        [TestMethod]
        public void SeedOpportunities_WithNullService_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                DevelopmentSampleData.SeedOpportunities(null!));
        }

        // Test that a new volunteer gets four requests: two fulfilled with hours, one declined and one pending.
        [TestMethod]
        public void AddSampleHistory_ForNewVolunteer_AddsFourRequestsWithMixedStatuses()
        {
            VolunteerRequestService requests = new();
            VolunteerOpportunityService opportunities = new();
            Guid volunteerId = Guid.NewGuid();

            bool added = DevelopmentSampleData.AddSampleHistory(requests, opportunities, volunteerId);

            Assert.IsTrue(added);
            IReadOnlyList<VolunteerRequest> history = requests.GetRequestsForVolunteer(volunteerId);
            Assert.HasCount(4, history);
            Assert.AreEqual(2, history.Count(r => r.Status == VolunteerRequestStatus.Fulfilled));
            Assert.AreEqual(1, history.Count(r => r.Status == VolunteerRequestStatus.Declined));
            Assert.AreEqual(1, history.Count(r => r.Status == VolunteerRequestStatus.Pending));
            Assert.AreEqual(7.5, history.Sum(r => r.HoursLogged));
        }

        // Test that asking for sample history a second time does nothing and reports false.
        [TestMethod]
        public void AddSampleHistory_CalledTwice_ReturnsFalseTheSecondTime()
        {
            VolunteerRequestService requests = new();
            VolunteerOpportunityService opportunities = new();
            Guid volunteerId = Guid.NewGuid();
            DevelopmentSampleData.AddSampleHistory(requests, opportunities, volunteerId);

            bool addedAgain = DevelopmentSampleData.AddSampleHistory(requests, opportunities, volunteerId);

            Assert.IsFalse(addedAgain);
            Assert.HasCount(4, requests.GetRequestsForVolunteer(volunteerId));
        }

        // Test that an empty volunteer id is rejected and nothing is created.
        [TestMethod]
        public void AddSampleHistory_WithEmptyVolunteerId_ReturnsFalseAndAddsNothing()
        {
            VolunteerRequestService requests = new();
            VolunteerOpportunityService opportunities = new();

            bool added = DevelopmentSampleData.AddSampleHistory(requests, opportunities, Guid.Empty);

            Assert.IsFalse(added);
            Assert.HasCount(0, requests.GetAllRequests());
        }

        // Test that two volunteers each get their own history without affecting each other.
        [TestMethod]
        public void AddSampleHistory_ForTwoVolunteers_KeepsHistoriesSeparate()
        {
            VolunteerRequestService requests = new();
            VolunteerOpportunityService opportunities = new();
            Guid first = Guid.NewGuid();
            Guid second = Guid.NewGuid();

            DevelopmentSampleData.AddSampleHistory(requests, opportunities, first);
            DevelopmentSampleData.AddSampleHistory(requests, opportunities, second);

            Assert.HasCount(4, requests.GetRequestsForVolunteer(first));
            Assert.HasCount(4, requests.GetRequestsForVolunteer(second));
            Assert.HasCount(8, requests.GetAllRequests());
            Assert.HasCount(4, opportunities.GetAllOpportunities());
        }

        // Test that sample history with no request service throws an ArgumentNullException.
        [TestMethod]
        public void AddSampleHistory_WithNullRequestService_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                DevelopmentSampleData.AddSampleHistory(null!, new VolunteerOpportunityService(), Guid.NewGuid()));
        }

        // Test that sample history with no opportunity service throws an ArgumentNullException.
        [TestMethod]
        public void AddSampleHistory_WithNullOpportunityService_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                DevelopmentSampleData.AddSampleHistory(new VolunteerRequestService(), null!, Guid.NewGuid()));
        }

        // Test that the sample history feeds the reporting totals (7.5 hours for the volunteer), so the demo dashboard has data.
        [TestMethod]
        public void AddSampleHistory_FeedsTheParticipationReport()
        {
            VolunteerRequestService requests = new();
            VolunteerOpportunityService opportunities = new();
            ReportingService reporting = new(requests, opportunities);
            Guid volunteerId = Guid.NewGuid();
            DevelopmentSampleData.AddSampleHistory(requests, opportunities, volunteerId);

            VolunteerParticipationSummary summary =
                reporting.GetVolunteerParticipationReport().Single(entry => entry.VolunteerId == volunteerId);

            Assert.AreEqual(4, summary.TotalRequests);
            Assert.AreEqual(2, summary.FulfilledRequests);
            Assert.AreEqual(7.5, summary.TotalHoursLogged);
        }
    }
}
