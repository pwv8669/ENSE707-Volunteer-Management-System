using WebApp.Services;

// This class contains unit tests for the WebApp's DevelopmentSampleData class, which fills the in-memory opportunity service with demo events for local development.
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
    }
}
