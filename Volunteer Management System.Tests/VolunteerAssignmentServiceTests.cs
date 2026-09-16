using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class VolunteerAssignmentServiceTests
    {
        private static User CreateVolunteer(
            string username)
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
                "coordinator",
                "coordinator@example.com",
                "Password123!",
                Role.Coordinator);
        }

        private static User CreateAdmin()
        {
            return User.Create(
                "admin",
                "admin@example.com",
                "Password123!",
                Role.Admin);
        }

        private static VolunteerOpportunity
            CreatePublishedOpportunity(
                VolunteerOpportunityService opportunityService,
                int volunteersNeeded = 5)
        {
            DateTime startTime =
                DateTime.UtcNow.AddDays(7);

            VolunteerOpportunity opportunity =
                opportunityService.CreateOpportunity(
                    "Beach Cleanup",
                    "Help clean the beach.",
                    "Mission Bay",
                    startTime,
                    startTime.AddHours(3),
                    "Teamwork",
                    volunteersNeeded);

            opportunityService.PublishOpportunity(
                opportunity.Id);

            return opportunity;
        }

        [TestMethod]
        public void GetPendingApplicationsForOpportunity_ReturnsPendingApplications()
        {
            VolunteerOpportunityService opportunityService = new();

            VolunteerOpportunity opportunity =
                CreatePublishedOpportunity(
                    opportunityService);

            VolunteerApplicationService applicationService =
                new(opportunityService);

            User volunteer =
                CreateVolunteer("volunteer1");

            applicationService.SubmitApplication(
                volunteer,
                opportunity.Id);

            VolunteerAssignmentService assignmentService =
                new(
                    opportunityService,
                    applicationService);

            var results =
                assignmentService
                    .GetPendingApplicationsForOpportunity(
                        opportunity.Id);

            Assert.HasCount(1, results);

            Assert.AreEqual(
                VolunteerApplicationStatus.Pending,
                results[0].Status);
        }

        [TestMethod]
        public void ApproveAndAssign_WithCoordinator_CreatesAssignment()
        {
            VolunteerOpportunityService opportunityService = new();

            VolunteerOpportunity opportunity =
                CreatePublishedOpportunity(
                    opportunityService);

            VolunteerApplicationService applicationService =
                new(opportunityService);

            User volunteer =
                CreateVolunteer("volunteer1");

            VolunteerApplication application =
                applicationService.SubmitApplication(
                    volunteer,
                    opportunity.Id);

            User coordinator =
                CreateCoordinator();

            VolunteerAssignmentService assignmentService =
                new(
                    opportunityService,
                    applicationService);

            VolunteerAssignment assignment =
                assignmentService.ApproveAndAssign(
                    coordinator,
                    application.Id);

            Assert.AreEqual(
                AssignmentStatus.Assigned,
                assignment.Status);

            Assert.AreEqual(
                volunteer.Id,
                assignment.VolunteerId);

            Assert.AreEqual(
                VolunteerApplicationStatus.Approved,
                application.Status);

            Assert.AreEqual(
                coordinator.Id,
                application.ReviewedByUserId);
        }

        [TestMethod]
        public void ApproveAndAssign_WithAdmin_CreatesAssignment()
        {
            VolunteerOpportunityService opportunityService = new();

            VolunteerOpportunity opportunity =
                CreatePublishedOpportunity(
                    opportunityService);

            VolunteerApplicationService applicationService =
                new(opportunityService);

            User volunteer =
                CreateVolunteer("volunteer1");

            VolunteerApplication application =
                applicationService.SubmitApplication(
                    volunteer,
                    opportunity.Id);

            VolunteerAssignmentService assignmentService =
                new(
                    opportunityService,
                    applicationService);

            VolunteerAssignment assignment =
                assignmentService.ApproveAndAssign(
                    CreateAdmin(),
                    application.Id);

            Assert.AreEqual(
                AssignmentStatus.Assigned,
                assignment.Status);
        }

        [TestMethod]
        public void ApproveAndAssign_WithVolunteerReviewer_ThrowsUnauthorizedAccessException()
        {
            VolunteerOpportunityService opportunityService = new();

            VolunteerOpportunity opportunity =
                CreatePublishedOpportunity(
                    opportunityService);

            VolunteerApplicationService applicationService =
                new(opportunityService);

            User applicant =
                CreateVolunteer("applicant");

            VolunteerApplication application =
                applicationService.SubmitApplication(
                    applicant,
                    opportunity.Id);

            User reviewer =
                CreateVolunteer("reviewer");

            VolunteerAssignmentService assignmentService =
                new(
                    opportunityService,
                    applicationService);

            Assert.ThrowsExactly<UnauthorizedAccessException>(() =>
                assignmentService.ApproveAndAssign(
                    reviewer,
                    application.Id));
        }

        [TestMethod]
        public void RejectApplication_WithCoordinator_SetsRejectedStatus()
        {
            VolunteerOpportunityService opportunityService = new();

            VolunteerOpportunity opportunity =
                CreatePublishedOpportunity(
                    opportunityService);

            VolunteerApplicationService applicationService =
                new(opportunityService);

            User volunteer =
                CreateVolunteer("volunteer1");

            VolunteerApplication application =
                applicationService.SubmitApplication(
                    volunteer,
                    opportunity.Id);

            User coordinator =
                CreateCoordinator();

            VolunteerAssignmentService assignmentService =
                new(
                    opportunityService,
                    applicationService);

            assignmentService.RejectApplication(
                coordinator,
                application.Id);

            Assert.AreEqual(
                VolunteerApplicationStatus.Rejected,
                application.Status);

            Assert.HasCount(
                0,
                assignmentService
                    .GetAssignmentsForOpportunity(
                        opportunity.Id));
        }

        [TestMethod]
        public void ApproveAndAssign_WhenCapacityReached_ThrowsException()
        {
            VolunteerOpportunityService opportunityService = new();

            VolunteerOpportunity opportunity =
                CreatePublishedOpportunity(
                    opportunityService,
                    1);

            VolunteerApplicationService applicationService =
                new(opportunityService);

            User firstVolunteer =
                CreateVolunteer("volunteer1");

            User secondVolunteer =
                CreateVolunteer("volunteer2");

            VolunteerApplication firstApplication =
                applicationService.SubmitApplication(
                    firstVolunteer,
                    opportunity.Id);

            VolunteerApplication secondApplication =
                applicationService.SubmitApplication(
                    secondVolunteer,
                    opportunity.Id);

            VolunteerAssignmentService assignmentService =
                new(
                    opportunityService,
                    applicationService);

            User coordinator =
                CreateCoordinator();

            assignmentService.ApproveAndAssign(
                coordinator,
                firstApplication.Id);

            InvalidOperationException exception =
                Assert.ThrowsExactly<InvalidOperationException>(() =>
                    assignmentService.ApproveAndAssign(
                        coordinator,
                        secondApplication.Id));

            StringAssert.Contains(
                exception.Message,
                "capacity");
        }

        [TestMethod]
        public void CancelAssignment_FreesOpportunityCapacity()
        {
            VolunteerOpportunityService opportunityService = new();

            VolunteerOpportunity opportunity =
                CreatePublishedOpportunity(
                    opportunityService,
                    1);

            VolunteerApplicationService applicationService =
                new(opportunityService);

            User firstVolunteer =
                CreateVolunteer("volunteer1");

            User secondVolunteer =
                CreateVolunteer("volunteer2");

            VolunteerApplication firstApplication =
                applicationService.SubmitApplication(
                    firstVolunteer,
                    opportunity.Id);

            VolunteerApplication secondApplication =
                applicationService.SubmitApplication(
                    secondVolunteer,
                    opportunity.Id);

            VolunteerAssignmentService assignmentService =
                new(
                    opportunityService,
                    applicationService);

            User coordinator =
                CreateCoordinator();

            VolunteerAssignment firstAssignment =
                assignmentService.ApproveAndAssign(
                    coordinator,
                    firstApplication.Id);

            assignmentService.CancelAssignment(
                coordinator,
                firstAssignment.Id);

            VolunteerAssignment secondAssignment =
                assignmentService.ApproveAndAssign(
                    coordinator,
                    secondApplication.Id);

            Assert.AreEqual(
                AssignmentStatus.Assigned,
                secondAssignment.Status);

            Assert.HasCount(
                1,
                assignmentService
                    .GetAssignmentsForOpportunity(
                        opportunity.Id));
        }

        [TestMethod]
        public void GetAssignmentsForVolunteer_ReturnsTheirAssignments()
        {
            VolunteerOpportunityService opportunityService = new();

            VolunteerOpportunity opportunity =
                CreatePublishedOpportunity(
                    opportunityService);

            VolunteerApplicationService applicationService =
                new(opportunityService);

            User volunteer =
                CreateVolunteer("volunteer1");

            VolunteerApplication application =
                applicationService.SubmitApplication(
                    volunteer,
                    opportunity.Id);

            VolunteerAssignmentService assignmentService =
                new(
                    opportunityService,
                    applicationService);

            assignmentService.ApproveAndAssign(
                CreateCoordinator(),
                application.Id);

            var results =
                assignmentService
                    .GetAssignmentsForVolunteer(
                        volunteer.Id);

            Assert.HasCount(1, results);

            Assert.AreEqual(
                volunteer.Id,
                results[0].VolunteerId);
        }
    }
}