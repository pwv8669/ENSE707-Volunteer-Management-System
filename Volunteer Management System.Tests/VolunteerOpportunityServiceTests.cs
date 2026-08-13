using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class VolunteerOpportunityServiceTests
    {
        [TestMethod]
        public void CreateOpportunity_WithValidDetails_StoresOpportunity()
        {
            VolunteerOpportunityService service = new();
            DateTime startTime = DateTime.UtcNow.AddDays(7);

            VolunteerOpportunity createdOpportunity =
                service.CreateOpportunity(
                    "Beach Cleanup",
                    "Help clean the beach.",
                    "Mission Bay",
                    startTime,
                    startTime.AddHours(3),
                    "Teamwork",
                    10);

            IReadOnlyList<VolunteerOpportunity> opportunities =
                service.GetAllOpportunities();

            Assert.HasCount(1, opportunities);
            Assert.AreEqual(
                createdOpportunity.Id,
                opportunities[0].Id);
        }

        [TestMethod]
        public void GetAllOpportunities_WithMultipleItems_ReturnsAllItems()
        {
            VolunteerOpportunityService service = new();
            DateTime startTime = DateTime.UtcNow.AddDays(7);

            service.CreateOpportunity(
                "Beach Cleanup",
                "Help clean the beach.",
                "Mission Bay",
                startTime,
                startTime.AddHours(3),
                "Teamwork",
                10);

            service.CreateOpportunity(
                "Food Drive",
                "Help collect donated food.",
                "Auckland CBD",
                startTime.AddDays(1),
                startTime.AddDays(1).AddHours(2),
                "Communication",
                5);

            IReadOnlyList<VolunteerOpportunity> opportunities =
                service.GetAllOpportunities();

            Assert.HasCount(2, opportunities);
        }

        [TestMethod]
        public void FindOpportunityById_WithExistingId_ReturnsOpportunity()
        {
            VolunteerOpportunityService service = new();
            DateTime startTime = DateTime.UtcNow.AddDays(7);

            VolunteerOpportunity createdOpportunity =
                service.CreateOpportunity(
                    "Tree Planting",
                    "Help plant native trees.",
                    "Western Springs",
                    startTime,
                    startTime.AddHours(4),
                    "Gardening",
                    15);

            VolunteerOpportunity? foundOpportunity =
                service.FindOpportunityById(createdOpportunity.Id);

            Assert.IsNotNull(foundOpportunity);
            Assert.AreEqual(
                createdOpportunity.Id,
                foundOpportunity.Id);
            Assert.AreEqual(
                "Tree Planting",
                foundOpportunity.Title);
        }

        [TestMethod]
        public void FindOpportunityById_WithUnknownId_ReturnsNull()
        {
            VolunteerOpportunityService service = new();

            VolunteerOpportunity? result =
                service.FindOpportunityById(Guid.NewGuid());

            Assert.IsNull(result);
        }

        [TestMethod]
        public void UpdateOpportunity_WithExistingId_UpdatesDetails()
        {
            VolunteerOpportunityService service = new();
            DateTime originalStart = DateTime.UtcNow.AddDays(7);

            VolunteerOpportunity opportunity =
                service.CreateOpportunity(
                    "Food Drive",
                    "Collect food.",
                    "Auckland CBD",
                    originalStart,
                    originalStart.AddHours(2),
                    "Communication",
                    5);

            DateTime updatedStart = DateTime.UtcNow.AddDays(14);

            service.UpdateOpportunity(
                opportunity.Id,
                "Community Food Drive",
                "Sort and distribute donated food.",
                "Manukau",
                updatedStart,
                updatedStart.AddHours(4),
                "Teamwork",
                12);

            VolunteerOpportunity? updatedOpportunity =
                service.FindOpportunityById(opportunity.Id);

            Assert.IsNotNull(updatedOpportunity);
            Assert.AreEqual(
                "Community Food Drive",
                updatedOpportunity.Title);
            Assert.AreEqual(
                "Manukau",
                updatedOpportunity.Location);
            Assert.AreEqual(
                12,
                updatedOpportunity.VolunteersNeeded);
        }

        [TestMethod]
        public void UpdateOpportunity_WithUnknownId_ThrowsException()
        {
            VolunteerOpportunityService service = new();
            DateTime startTime = DateTime.UtcNow.AddDays(7);

            KeyNotFoundException exception =
                Assert.ThrowsExactly<KeyNotFoundException>(() =>
                    service.UpdateOpportunity(
                        Guid.NewGuid(),
                        "Food Drive",
                        "Collect donated food.",
                        "Auckland",
                        startTime,
                        startTime.AddHours(2),
                        "Teamwork",
                        5));

            StringAssert.Contains(
                exception.Message,
                "was not found");
        }

        [TestMethod]
        public void DeleteOpportunity_WithExistingId_RemovesOpportunity()
        {
            VolunteerOpportunityService service = new();
            DateTime startTime = DateTime.UtcNow.AddDays(7);

            VolunteerOpportunity opportunity =
                service.CreateOpportunity(
                    "Beach Cleanup",
                    "Help clean the beach.",
                    "Mission Bay",
                    startTime,
                    startTime.AddHours(3),
                    "Teamwork",
                    10);

            bool result =
                service.DeleteOpportunity(opportunity.Id);

            Assert.IsTrue(result);
            Assert.HasCount(
                0,
                service.GetAllOpportunities());
            Assert.IsNull(
                service.FindOpportunityById(opportunity.Id));
        }

        [TestMethod]
        public void DeleteOpportunity_WithUnknownId_ReturnsFalse()
        {
            VolunteerOpportunityService service = new();

            bool result =
                service.DeleteOpportunity(Guid.NewGuid());

            Assert.IsFalse(result);
            Assert.HasCount(
                0,
                service.GetAllOpportunities());
        }
    }
}