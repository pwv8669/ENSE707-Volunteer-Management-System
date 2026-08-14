using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class VolunteerRequestTests
    {
        [TestMethod]
        public void Create_WithValidIds_CreatesPendingRequest()
        {
            Guid volunteerId = Guid.NewGuid();
            Guid opportunityId = Guid.NewGuid();

            VolunteerRequest request =
                VolunteerRequest.Create(volunteerId, opportunityId);

            Assert.AreNotEqual(Guid.Empty, request.Id);
            Assert.AreEqual(volunteerId, request.VolunteerId);
            Assert.AreEqual(opportunityId, request.OpportunityId);
            Assert.AreEqual(
                VolunteerRequestStatus.Pending,
                request.Status);
            Assert.IsNull(request.RespondedAt);
            Assert.AreEqual(0.0, request.HoursLogged);
        }

        [TestMethod]
        public void Create_WithEmptyVolunteerId_ThrowsArgumentException()
        {
            ArgumentException exception =
                Assert.ThrowsExactly<ArgumentException>(() =>
                    VolunteerRequest.Create(Guid.Empty, Guid.NewGuid()));

            StringAssert.Contains(
                exception.Message,
                "Volunteer id is required");
        }

        [TestMethod]
        public void Create_WithEmptyOpportunityId_ThrowsArgumentException()
        {
            ArgumentException exception =
                Assert.ThrowsExactly<ArgumentException>(() =>
                    VolunteerRequest.Create(Guid.NewGuid(), Guid.Empty));

            StringAssert.Contains(
                exception.Message,
                "Opportunity id is required");
        }
    }
}
