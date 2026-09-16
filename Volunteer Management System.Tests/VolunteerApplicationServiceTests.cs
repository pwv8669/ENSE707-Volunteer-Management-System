using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class VolunteerApplicationServiceTests
    {
        private static User CreateVolunteer(
            string username = "volunteer1")
        {
            return User.Create(
                username,
                $"{username}@example.com",
                "Password123!",
                Role.Volunteer);
        }

        private static User CreateCoordinator()
        {
            return User.Create(
                "coordinator1",
                "coordinator@example.com",
                "Password123!",
                Role.Coordinator);
        }

        private static VolunteerOpportunity CreatePublishedOpportunity(
            VolunteerOpportunityService opportunityService,
            string title = "Beach Cleanup")
        {
            DateTime startTime =
                DateTime.UtcNow.AddDays(7);

            VolunteerOpportunity opportunity =
                opportunityService.CreateOpportunity(
                    title,
                    "Help with a community volunteer event.",
                    "Auckland",
                    startTime,
                    startTime.AddHours(3),
                    "Teamwork",
                    10);

            opportunityService.PublishOpportunity(
                opportunity.Id);

            return opportunity;
        }

        [TestMethod]
        public void BrowseAvailableOpportunities_ReturnsPublishedOpportunities()
        {
            VolunteerOpportunityService opportunityService = new();

            VolunteerOpportunity published =
                CreatePublishedOpportunity(
                    opportunityService);

            VolunteerApplicationService applicationService =
                new(opportunityService);

            IReadOnlyList<VolunteerOpportunity> results =
                applicationService
                    .BrowseAvailableOpportunities();

            Assert.HasCount(1, results);

            Assert.AreEqual(
                published.Id,
                results[0].Id);

            Assert.AreEqual(
                OpportunityStatus.Published,
                results[0].Status);
        }

        [TestMethod]
        public void BrowseAvailableOpportunities_DoesNotReturnDrafts()
        {
            VolunteerOpportunityService opportunityService = new();

            DateTime startTime =
                DateTime.UtcNow.AddDays(7);

            opportunityService.CreateOpportunity(
                "Food Drive",
                "Collect donated food.",
                "Auckland CBD",
                startTime,
                startTime.AddHours(2),
                "Communication",
                5);

            VolunteerApplicationService applicationService =
                new(opportunityService);

            IReadOnlyList<VolunteerOpportunity> results =
                applicationService
                    .BrowseAvailableOpportunities();

            Assert.HasCount(0, results);
        }

        [TestMethod]
        public void SubmitApplication_WithPublishedOpportunity_CreatesPendingApplication()
        {
            VolunteerOpportunityService opportunityService = new();

            VolunteerOpportunity opportunity =
                CreatePublishedOpportunity(
                    opportunityService);

            VolunteerApplicationService applicationService =
                new(opportunityService);

            User volunteer =
                CreateVolunteer();

            VolunteerApplication application =
                applicationService.SubmitApplication(
                    volunteer,
                    opportunity.Id);

            Assert.AreEqual(
                volunteer.Id,
                application.VolunteerId);

            Assert.AreEqual(
                opportunity.Id,
                application.OpportunityId);

            Assert.AreEqual(
                VolunteerApplicationStatus.Pending,
                application.Status);

            Assert.HasCount(
                1,
                applicationService.GetAllApplications());
        }

        [TestMethod]
        public void SubmitApplication_WithCoordinator_ThrowsUnauthorizedAccessException()
        {
            VolunteerOpportunityService opportunityService = new();

            VolunteerOpportunity opportunity =
                CreatePublishedOpportunity(
                    opportunityService);

            VolunteerApplicationService applicationService =
                new(opportunityService);

            User coordinator =
                CreateCoordinator();

            UnauthorizedAccessException exception =
                Assert.ThrowsExactly<UnauthorizedAccessException>(() =>
                    applicationService.SubmitApplication(
                        coordinator,
                        opportunity.Id));

            StringAssert.Contains(
                exception.Message,
                "Only volunteers");
        }

        [TestMethod]
        public void SubmitApplication_WithUnknownOpportunity_ThrowsKeyNotFoundException()
        {
            VolunteerOpportunityService opportunityService = new();

            VolunteerApplicationService applicationService =
                new(opportunityService);

            User volunteer =
                CreateVolunteer();

            KeyNotFoundException exception =
                Assert.ThrowsExactly<KeyNotFoundException>(() =>
                    applicationService.SubmitApplication(
                        volunteer,
                        Guid.NewGuid()));

            StringAssert.Contains(
                exception.Message,
                "was not found");
        }

        [TestMethod]
        public void SubmitApplication_WithDraftOpportunity_ThrowsInvalidOperationException()
        {
            VolunteerOpportunityService opportunityService = new();

            DateTime startTime =
                DateTime.UtcNow.AddDays(7);

            VolunteerOpportunity opportunity =
                opportunityService.CreateOpportunity(
                    "Food Drive",
                    "Collect donated food.",
                    "Auckland CBD",
                    startTime,
                    startTime.AddHours(2),
                    "Communication",
                    5);

            VolunteerApplicationService applicationService =
                new(opportunityService);

            User volunteer =
                CreateVolunteer();

            InvalidOperationException exception =
                Assert.ThrowsExactly<InvalidOperationException>(() =>
                    applicationService.SubmitApplication(
                        volunteer,
                        opportunity.Id));

            StringAssert.Contains(
                exception.Message,
                "published opportunities");
        }

        [TestMethod]
        public void SubmitApplication_WithArchivedOpportunity_ThrowsInvalidOperationException()
        {
            VolunteerOpportunityService opportunityService = new();

            VolunteerOpportunity opportunity =
                CreatePublishedOpportunity(
                    opportunityService);

            opportunityService.ArchiveOpportunity(
                opportunity.Id);

            VolunteerApplicationService applicationService =
                new(opportunityService);

            User volunteer =
                CreateVolunteer();

            InvalidOperationException exception =
                Assert.ThrowsExactly<InvalidOperationException>(() =>
                    applicationService.SubmitApplication(
                        volunteer,
                        opportunity.Id));

            StringAssert.Contains(
                exception.Message,
                "published opportunities");
        }

        [TestMethod]
        public void SubmitApplication_WhenAlreadyApplied_ThrowsInvalidOperationException()
        {
            VolunteerOpportunityService opportunityService = new();

            VolunteerOpportunity opportunity =
                CreatePublishedOpportunity(
                    opportunityService);

            VolunteerApplicationService applicationService =
                new(opportunityService);

            User volunteer =
                CreateVolunteer();

            applicationService.SubmitApplication(
                volunteer,
                opportunity.Id);

            InvalidOperationException exception =
                Assert.ThrowsExactly<InvalidOperationException>(() =>
                    applicationService.SubmitApplication(
                        volunteer,
                        opportunity.Id));

            StringAssert.Contains(
                exception.Message,
                "already applied");
        }

        [TestMethod]
        public void GetApplicationsForVolunteer_ReturnsOnlyTheirApplications()
        {
            VolunteerOpportunityService opportunityService = new();

            VolunteerOpportunity opportunity =
                CreatePublishedOpportunity(
                    opportunityService);

            VolunteerApplicationService applicationService =
                new(opportunityService);

            User firstVolunteer =
                CreateVolunteer("volunteer1");

            User secondVolunteer =
                CreateVolunteer("volunteer2");

            applicationService.SubmitApplication(
                firstVolunteer,
                opportunity.Id);

            applicationService.SubmitApplication(
                secondVolunteer,
                opportunity.Id);

            IReadOnlyList<VolunteerApplication> results =
                applicationService.GetApplicationsForVolunteer(
                    firstVolunteer.Id);

            Assert.HasCount(1, results);

            Assert.AreEqual(
                firstVolunteer.Id,
                results[0].VolunteerId);
        }

        [TestMethod]
        public void FindApplicationById_WithExistingApplication_ReturnsApplication()
        {
            VolunteerOpportunityService opportunityService = new();

            VolunteerOpportunity opportunity =
                CreatePublishedOpportunity(
                    opportunityService);

            VolunteerApplicationService applicationService =
                new(opportunityService);

            User volunteer =
                CreateVolunteer();

            VolunteerApplication created =
                applicationService.SubmitApplication(
                    volunteer,
                    opportunity.Id);

            VolunteerApplication? found =
                applicationService.FindApplicationById(
                    created.Id);

            Assert.IsNotNull(found);

            Assert.AreEqual(
                created.Id,
                found.Id);
        }

        [TestMethod]
        public void GetApplicationStatus_WithOwnApplication_ReturnsPending()
        {
            VolunteerOpportunityService opportunityService = new();

            VolunteerOpportunity opportunity =
                CreatePublishedOpportunity(
                    opportunityService);

            VolunteerApplicationService applicationService =
                new(opportunityService);

            User volunteer =
                CreateVolunteer();

            VolunteerApplication application =
                applicationService.SubmitApplication(
                    volunteer,
                    opportunity.Id);

            VolunteerApplicationStatus status =
                applicationService.GetApplicationStatus(
                    volunteer,
                    application.Id);

            Assert.AreEqual(
                VolunteerApplicationStatus.Pending,
                status);
        }

        [TestMethod]
        public void GetApplicationStatus_WithAnotherVolunteersApplication_ThrowsUnauthorizedAccessException()
        {
            VolunteerOpportunityService opportunityService = new();

            VolunteerOpportunity opportunity =
                CreatePublishedOpportunity(
                    opportunityService);

            VolunteerApplicationService applicationService =
                new(opportunityService);

            User firstVolunteer =
                CreateVolunteer("volunteer1");

            User secondVolunteer =
                CreateVolunteer("volunteer2");

            VolunteerApplication application =
                applicationService.SubmitApplication(
                    firstVolunteer,
                    opportunity.Id);

            UnauthorizedAccessException exception =
                Assert.ThrowsExactly<UnauthorizedAccessException>(() =>
                    applicationService.GetApplicationStatus(
                        secondVolunteer,
                        application.Id));

            StringAssert.Contains(
                exception.Message,
                "their own application");
        }

        [TestMethod]
        public void GetApplicationStatus_WithUnknownApplication_ThrowsKeyNotFoundException()
        {
            VolunteerOpportunityService opportunityService = new();

            VolunteerApplicationService applicationService =
                new(opportunityService);

            User volunteer =
                CreateVolunteer();

            KeyNotFoundException exception =
                Assert.ThrowsExactly<KeyNotFoundException>(() =>
                    applicationService.GetApplicationStatus(
                        volunteer,
                        Guid.NewGuid()));

            StringAssert.Contains(
                exception.Message,
                "was not found");
        }
    }
}