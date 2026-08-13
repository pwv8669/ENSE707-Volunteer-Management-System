using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class VolunteerOpportunityTests
    {
        [TestMethod]
        public void Create_WithValidDetails_CreatesDraftOpportunity()
        {
            // Arrange
            DateTime startTime = DateTime.UtcNow.AddDays(7);
            DateTime endTime = startTime.AddHours(3);

            // Act
            VolunteerOpportunity opportunity = VolunteerOpportunity.Create(
                "Beach Cleanup",
                "Help clean rubbish from the beach.",
                "Mission Bay",
                startTime,
                endTime,
                "Teamwork",
                10);

            // Assert
            Assert.AreNotEqual(Guid.Empty, opportunity.Id);
            Assert.AreEqual("Beach Cleanup", opportunity.Title);
            Assert.AreEqual("Mission Bay", opportunity.Location);
            Assert.AreEqual(startTime, opportunity.StartDateTime);
            Assert.AreEqual(endTime, opportunity.EndDateTime);
            Assert.AreEqual("Teamwork", opportunity.RequiredSkills);
            Assert.AreEqual(10, opportunity.VolunteersNeeded);
            Assert.AreEqual(OpportunityStatus.Draft, opportunity.Status);
        }

        [TestMethod]
        public void Create_TrimsTextValues()
        {
            // Act
            VolunteerOpportunity opportunity = VolunteerOpportunity.Create(
                "  Food Drive  ",
                "  Collect donated food.  ",
                "  Auckland CBD  ",
                DateTime.UtcNow.AddDays(5),
                DateTime.UtcNow.AddDays(5).AddHours(2),
                "  Communication  ",
                5);

            // Assert
            Assert.AreEqual("Food Drive", opportunity.Title);
            Assert.AreEqual("Collect donated food.", opportunity.Description);
            Assert.AreEqual("Auckland CBD", opportunity.Location);
            Assert.AreEqual("Communication", opportunity.RequiredSkills);
        }
    }
}