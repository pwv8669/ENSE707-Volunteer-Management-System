
// Purpose:
// Contains unit tests for the VolunteerOpportunity model.
//
// These tests verify that volunteer opportunities can be created and updated
// correctly, that invalid opportunity information is rejected, and that the
// Draft, Published and Archived status transitions behave as expected.


using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    // Tests the core behaviour and validation rules of VolunteerOpportunity.
    [TestClass]
    public class VolunteerOpportunityTests
    {
        // Verifies that valid opportunity information creates an opportunity
        // with the correct details and an initial Draft status.
        [TestMethod]
        public void Create_WithValidDetails_CreatesDraftOpportunity()
        {
            DateTime startTime = DateTime.UtcNow.AddDays(7);
            DateTime endTime = startTime.AddHours(3);

            VolunteerOpportunity opportunity =
                VolunteerOpportunity.Create(
                    "Beach Cleanup",
                    "Help clean rubbish from the beach.",
                    "Mission Bay",
                    startTime,
                    endTime,
                    "Teamwork",
                    10);

            Assert.AreNotEqual(Guid.Empty, opportunity.Id);
            Assert.AreEqual("Beach Cleanup", opportunity.Title);
            Assert.AreEqual(
                "Help clean rubbish from the beach.",
                opportunity.Description);
            Assert.AreEqual("Mission Bay", opportunity.Location);
            Assert.AreEqual(startTime, opportunity.StartDateTime);
            Assert.AreEqual(endTime, opportunity.EndDateTime);
            Assert.AreEqual("Teamwork", opportunity.RequiredSkills);
            Assert.AreEqual(10, opportunity.VolunteersNeeded);
            Assert.AreEqual(
                OpportunityStatus.Draft,
                opportunity.Status);
        }

        // Verifies that unnecessary spaces around text values are removed
        // when a new volunteer opportunity is created.
        [TestMethod]
        public void Create_TrimsTextValues()
        {
            DateTime startTime = DateTime.UtcNow.AddDays(5);

            VolunteerOpportunity opportunity =
                VolunteerOpportunity.Create(
                    "  Food Drive  ",
                    "  Collect donated food.  ",
                    "  Auckland CBD  ",
                    startTime,
                    startTime.AddHours(2),
                    "  Communication  ",
                    5);

            Assert.AreEqual("Food Drive", opportunity.Title);
            Assert.AreEqual(
                "Collect donated food.",
                opportunity.Description);
            Assert.AreEqual(
                "Auckland CBD",
                opportunity.Location);
            Assert.AreEqual(
                "Communication",
                opportunity.RequiredSkills);
        }

        // Verifies that an opportunity cannot be created without a title.
        [TestMethod]
        public void Create_WithMissingTitle_ThrowsArgumentException()
        {
            DateTime startTime = DateTime.UtcNow.AddDays(5);

            ArgumentException exception =
                Assert.ThrowsExactly<ArgumentException>(() =>
                    VolunteerOpportunity.Create(
                        "",
                        "Help at an event.",
                        "Auckland",
                        startTime,
                        startTime.AddHours(2),
                        "Communication",
                        5));

            StringAssert.Contains(
                exception.Message,
                "Title is required");
        }

        // Verifies that the end time of an opportunity must occur after
        // its start time.
        [TestMethod]
        public void Create_WithEndBeforeStart_ThrowsArgumentException()
        {
            DateTime startTime = DateTime.UtcNow.AddDays(5);
            DateTime endTime = startTime.AddHours(-1);

            ArgumentException exception =
                Assert.ThrowsExactly<ArgumentException>(() =>
                    VolunteerOpportunity.Create(
                        "Food Drive",
                        "Help collect food.",
                        "Auckland",
                        startTime,
                        endTime,
                        "Teamwork",
                        5));

            StringAssert.Contains(
                exception.Message,
                "End date and time must be after");
        }

        // Verifies that an opportunity must request at least one volunteer.
        [TestMethod]
        public void Create_WithZeroVolunteers_ThrowsArgumentException()
        {
            DateTime startTime = DateTime.UtcNow.AddDays(5);

            ArgumentException exception =
                Assert.ThrowsExactly<ArgumentException>(() =>
                    VolunteerOpportunity.Create(
                        "Food Drive",
                        "Help collect food.",
                        "Auckland",
                        startTime,
                        startTime.AddHours(2),
                        "Teamwork",
                        0));

            StringAssert.Contains(
                exception.Message,
                "Volunteers needed must be greater than zero");
        }

        // Verifies that valid replacement details correctly update an
        // existing volunteer opportunity.
        [TestMethod]
        public void UpdateDetails_WithValidDetails_UpdatesOpportunity()
        {
            DateTime originalStart = DateTime.UtcNow.AddDays(5);

            VolunteerOpportunity opportunity =
                VolunteerOpportunity.Create(
                    "Food Drive",
                    "Collect donated food.",
                    "Auckland CBD",
                    originalStart,
                    originalStart.AddHours(2),
                    "Communication",
                    5);

            DateTime updatedStart = DateTime.UtcNow.AddDays(10);

            opportunity.UpdateDetails(
                "Community Food Drive",
                "Sort and distribute donated food.",
                "Manukau",
                updatedStart,
                updatedStart.AddHours(4),
                "Teamwork",
                12);

            Assert.AreEqual(
                "Community Food Drive",
                opportunity.Title);
            Assert.AreEqual(
                "Sort and distribute donated food.",
                opportunity.Description);
            Assert.AreEqual("Manukau", opportunity.Location);
            Assert.AreEqual(updatedStart, opportunity.StartDateTime);
            Assert.AreEqual(
                updatedStart.AddHours(4),
                opportunity.EndDateTime);
            Assert.AreEqual("Teamwork", opportunity.RequiredSkills);
            Assert.AreEqual(12, opportunity.VolunteersNeeded);
            Assert.AreEqual(
                OpportunityStatus.Draft,
                opportunity.Status);
        }

        // Verifies that a Draft opportunity can successfully be published.
        [TestMethod]
        public void Publish_WithDraftOpportunity_SetsPublishedStatus()
        {
            DateTime startTime = DateTime.UtcNow.AddDays(5);

            VolunteerOpportunity opportunity =
                VolunteerOpportunity.Create(
                    "Beach Cleanup",
                    "Clean the beach.",
                    "Mission Bay",
                    startTime,
                    startTime.AddHours(2),
                    "Teamwork",
                    10);

            opportunity.Publish();

            Assert.AreEqual(
                OpportunityStatus.Published,
                opportunity.Status);
        }

        // Verifies that a Published opportunity can be archived.
        [TestMethod]
        public void Archive_WithPublishedOpportunity_SetsArchivedStatus()
        {
            DateTime startTime = DateTime.UtcNow.AddDays(5);

            VolunteerOpportunity opportunity =
                VolunteerOpportunity.Create(
                    "Beach Cleanup",
                    "Clean the beach.",
                    "Mission Bay",
                    startTime,
                    startTime.AddHours(2),
                    "Teamwork",
                    10);

            opportunity.Publish();
            opportunity.Archive();

            Assert.AreEqual(
                OpportunityStatus.Archived,
                opportunity.Status);
        }

        // Verifies that a Draft opportunity can also be archived without
        // first being published.
        [TestMethod]
        public void Archive_WithDraftOpportunity_SetsArchivedStatus()
        {
            DateTime startTime = DateTime.UtcNow.AddDays(5);

            VolunteerOpportunity opportunity =
                VolunteerOpportunity.Create(
                    "Food Drive",
                    "Collect donated food.",
                    "Auckland CBD",
                    startTime,
                    startTime.AddHours(2),
                    "Communication",
                    5);

            opportunity.Archive();

            Assert.AreEqual(
                OpportunityStatus.Archived,
                opportunity.Status);
        }

        // Verifies that an Archived opportunity cannot be published again.
        [TestMethod]
        public void Publish_WithArchivedOpportunity_ThrowsException()
        {
            DateTime startTime = DateTime.UtcNow.AddDays(5);

            VolunteerOpportunity opportunity =
                VolunteerOpportunity.Create(
                    "Beach Cleanup",
                    "Clean the beach.",
                    "Mission Bay",
                    startTime,
                    startTime.AddHours(2),
                    "Teamwork",
                    10);

            opportunity.Archive();

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                opportunity.Publish());
        }

        // Verifies that an already Published opportunity cannot be
        // published for a second time.
        [TestMethod]
        public void Publish_WithAlreadyPublishedOpportunity_ThrowsException()
        {
            DateTime startTime = DateTime.UtcNow.AddDays(5);

            VolunteerOpportunity opportunity =
                VolunteerOpportunity.Create(
                    "Tree Planting",
                    "Plant native trees.",
                    "Western Springs",
                    startTime,
                    startTime.AddHours(3),
                    "Gardening",
                    8);

            opportunity.Publish();

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                opportunity.Publish());
        }

        // Verifies that an Archived opportunity cannot be archived again.
        [TestMethod]
        public void Archive_WithAlreadyArchivedOpportunity_ThrowsException()
        {
            DateTime startTime = DateTime.UtcNow.AddDays(5);

            VolunteerOpportunity opportunity =
                VolunteerOpportunity.Create(
                    "Community Event",
                    "Help run the event.",
                    "Auckland",
                    startTime,
                    startTime.AddHours(3),
                    "Communication",
                    6);

            opportunity.Archive();

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                opportunity.Archive());
        }
    }
}