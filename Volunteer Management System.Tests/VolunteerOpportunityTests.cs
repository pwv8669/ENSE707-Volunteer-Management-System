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
    }
}