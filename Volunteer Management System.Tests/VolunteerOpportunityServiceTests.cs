
// Purpose:
// Contains unit tests for the VolunteerOpportunityService.
//
// These tests verify that volunteer opportunities can be created, retrieved,
// updated, deleted, published and archived through the service. They also
// confirm that volunteers browsing available opportunities only receive
// opportunities with Published status.


using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    // Tests the management operations provided by VolunteerOpportunityService.
    [TestClass]
    public class VolunteerOpportunityServiceTests
    {
        // Verifies that creating a valid opportunity stores it in the service
        // and makes it available through GetAllOpportunities.
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

        // Verifies that GetAllOpportunities returns every opportunity
        // currently stored by the service.
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

        // Verifies that an opportunity can be found using its unique ID.
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

        // Verifies that searching with an unknown opportunity ID
        // returns null instead of an unrelated opportunity.
        [TestMethod]
        public void FindOpportunityById_WithUnknownId_ReturnsNull()
        {
            VolunteerOpportunityService service = new();

            VolunteerOpportunity? result =
                service.FindOpportunityById(Guid.NewGuid());

            Assert.IsNull(result);
        }

        // Verifies that an existing opportunity can have its details updated
        // through the service.
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

        // Verifies that trying to update an opportunity that does not exist
        // produces a KeyNotFoundException.
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

        // Verifies that an existing opportunity can be deleted and is no
        // longer available through the service afterwards.
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

        // Verifies that deleting an unknown opportunity safely returns false
        // and does not remove anything from the service.
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

        // Verifies that the service can publish an existing Draft
        // opportunity and change its status to Published.
        [TestMethod]
        public void PublishOpportunity_WithExistingOpportunity_PublishesOpportunity()
        {
            VolunteerOpportunityService service = new();
            DateTime startTime = DateTime.UtcNow.AddDays(5);

            VolunteerOpportunity opportunity =
                service.CreateOpportunity(
                    "Beach Cleanup",
                    "Clean the beach.",
                    "Mission Bay",
                    startTime,
                    startTime.AddHours(2),
                    "Teamwork",
                    10);

            service.PublishOpportunity(opportunity.Id);

            Assert.AreEqual(
                OpportunityStatus.Published,
                opportunity.Status);
        }

        // Verifies that attempting to publish an unknown opportunity
        // produces a KeyNotFoundException.
        [TestMethod]
        public void PublishOpportunity_WithUnknownId_ThrowsException()
        {
            VolunteerOpportunityService service = new();

            KeyNotFoundException exception =
                Assert.ThrowsExactly<KeyNotFoundException>(() =>
                    service.PublishOpportunity(Guid.NewGuid()));

            StringAssert.Contains(
                exception.Message,
                "was not found");
        }

        // Verifies that a Published opportunity can be archived through
        // the service.
        [TestMethod]
        public void ArchiveOpportunity_WithPublishedOpportunity_ArchivesOpportunity()
        {
            VolunteerOpportunityService service = new();
            DateTime startTime = DateTime.UtcNow.AddDays(5);

            VolunteerOpportunity opportunity =
                service.CreateOpportunity(
                    "Tree Planting",
                    "Plant native trees.",
                    "Auckland",
                    startTime,
                    startTime.AddHours(3),
                    "Gardening",
                    8);

            service.PublishOpportunity(opportunity.Id);
            service.ArchiveOpportunity(opportunity.Id);

            Assert.AreEqual(
                OpportunityStatus.Archived,
                opportunity.Status);
        }

        // Verifies that attempting to archive an unknown opportunity
        // produces a KeyNotFoundException.
        [TestMethod]
        public void ArchiveOpportunity_WithUnknownId_ThrowsException()
        {
            VolunteerOpportunityService service = new();

            KeyNotFoundException exception =
                Assert.ThrowsExactly<KeyNotFoundException>(() =>
                    service.ArchiveOpportunity(Guid.NewGuid()));

            StringAssert.Contains(
                exception.Message,
                "was not found");
        }

        // Verifies that browsing published opportunities returns only
        // opportunities whose status is Published.
        [TestMethod]
        public void GetPublishedOpportunities_ReturnsOnlyPublishedItems()
        {
            VolunteerOpportunityService service = new();
            DateTime startTime = DateTime.UtcNow.AddDays(5);

            VolunteerOpportunity publishedOpportunity =
                service.CreateOpportunity(
                    "Beach Cleanup",
                    "Clean the beach.",
                    "Mission Bay",
                    startTime,
                    startTime.AddHours(2),
                    "Teamwork",
                    10);

            service.CreateOpportunity(
                "Food Drive",
                "Collect donated food.",
                "Auckland CBD",
                startTime.AddDays(1),
                startTime.AddDays(1).AddHours(2),
                "Communication",
                5);

            service.PublishOpportunity(
                publishedOpportunity.Id);

            IReadOnlyList<VolunteerOpportunity> results =
                service.GetPublishedOpportunities();

            Assert.HasCount(1, results);
            Assert.AreEqual(
                publishedOpportunity.Id,
                results[0].Id);
            Assert.AreEqual(
                OpportunityStatus.Published,
                results[0].Status);
        }

        // Verifies that opportunities still in Draft status are not shown
        // when volunteers browse published opportunities.
        [TestMethod]
        public void GetPublishedOpportunities_DoesNotReturnDraftOpportunity()
        {
            VolunteerOpportunityService service = new();
            DateTime startTime = DateTime.UtcNow.AddDays(5);

            service.CreateOpportunity(
                "Food Drive",
                "Collect donated food.",
                "Auckland CBD",
                startTime,
                startTime.AddHours(2),
                "Communication",
                5);

            IReadOnlyList<VolunteerOpportunity> results =
                service.GetPublishedOpportunities();

            Assert.HasCount(0, results);
        }

        // Verifies that archived opportunities are removed from the list
        // of opportunities available for volunteers to browse.
        [TestMethod]
        public void GetPublishedOpportunities_DoesNotReturnArchivedOpportunity()
        {
            VolunteerOpportunityService service = new();
            DateTime startTime = DateTime.UtcNow.AddDays(5);

            VolunteerOpportunity opportunity =
                service.CreateOpportunity(
                    "Beach Cleanup",
                    "Clean the beach.",
                    "Mission Bay",
                    startTime,
                    startTime.AddHours(2),
                    "Teamwork",
                    10);

            service.PublishOpportunity(opportunity.Id);
            service.ArchiveOpportunity(opportunity.Id);

            IReadOnlyList<VolunteerOpportunity> results =
                service.GetPublishedOpportunities();

            Assert.HasCount(0, results);
        }
    }
}