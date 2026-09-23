
// Purpose:
// Contains unit tests for VolunteerApplicationService.
//
// These tests verify the complete volunteer application workflow, including:
// browsing published opportunities, submitting applications, role validation,
// preventing duplicate applications, retrieving applications and checking
// application status.
//
// The tests also verify that volunteers cannot apply to Draft or Archived
// opportunities and cannot access another volunteer's application status.


using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    // Tests the business rules provided by VolunteerApplicationService.
    [TestClass]
    public class VolunteerApplicationServiceTests
    {
        // Creates a Volunteer user for use across application service tests.
        private static User CreateVolunteer(
            string username = "volunteer1")
        {
            return User.Create(
                username,
                $"{username}@example.com",
                Role.Volunteer);
        }

        // Creates a Coordinator user for testing role restrictions.
        private static User CreateCoordinator()
        {
            return User.Create(
                "coordinator1",
                "coordinator@example.com",
                Role.Coordinator);
        }

        // Creates and publishes a volunteer opportunity for tests that
        // require an opportunity that is available for applications.
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

        // Verifies that volunteers browsing available opportunities receive
        // opportunities whose status is Published.
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

        // Verifies that Draft opportunities are hidden when volunteers
        // browse opportunities available for applications.
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

        // Verifies that a Volunteer can submit an application for a
        // Published opportunity and that it begins with Pending status.
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

        // Verifies that a Coordinator cannot submit a volunteer application.
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

        // Verifies that an application cannot be submitted for an
        // opportunity ID that does not exist.
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

        // Verifies that applications cannot be submitted for Draft
        // opportunities because they are not yet available to volunteers.
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

        // Verifies that applications cannot be submitted for opportunities
        // that have already been Archived.
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

        // Verifies that a volunteer cannot submit multiple applications
        // for the same opportunity.
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

        // Verifies that retrieving applications for a volunteer returns
        // only applications belonging to that volunteer.
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

        // Verifies that an existing application can be found using its
        // unique application ID.
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

        // Verifies that a volunteer can retrieve the Pending status of
        // their own application.
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

        // Verifies that one volunteer cannot view another volunteer's
        // application status.
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

        // Verifies that requesting the status of an application that does
        // not exist results in a KeyNotFoundException.
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
