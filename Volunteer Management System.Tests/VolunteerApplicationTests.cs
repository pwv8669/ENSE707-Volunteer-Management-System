using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class VolunteerApplicationTests
    {
        [TestMethod]
        public void Create_WithValidIds_CreatesPendingApplication()
        {
            Guid volunteerId = Guid.NewGuid();
            Guid opportunityId = Guid.NewGuid();

            VolunteerApplication application =
                VolunteerApplication.Create(
                    volunteerId,
                    opportunityId);

            Assert.AreNotEqual(
                Guid.Empty,
                application.Id);

            Assert.AreEqual(
                volunteerId,
                application.VolunteerId);

            Assert.AreEqual(
                opportunityId,
                application.OpportunityId);

            Assert.AreEqual(
                VolunteerApplicationStatus.Pending,
                application.Status);

            Assert.IsTrue(
                application.AppliedAt <= DateTime.UtcNow);
        }

        [TestMethod]
        public void Create_WithEmptyVolunteerId_ThrowsArgumentException()
        {
            Guid opportunityId = Guid.NewGuid();

            ArgumentException exception =
                Assert.ThrowsExactly<ArgumentException>(() =>
                    VolunteerApplication.Create(
                        Guid.Empty,
                        opportunityId));

            StringAssert.Contains(
                exception.Message,
                "Volunteer id is required");
        }

        [TestMethod]
        public void Create_WithEmptyOpportunityId_ThrowsArgumentException()
        {
            Guid volunteerId = Guid.NewGuid();

            ArgumentException exception =
                Assert.ThrowsExactly<ArgumentException>(() =>
                    VolunteerApplication.Create(
                        volunteerId,
                        Guid.Empty));

            StringAssert.Contains(
                exception.Message,
                "Opportunity id is required");
        }
    }
}