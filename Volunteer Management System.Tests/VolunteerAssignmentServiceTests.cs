
// Purpose:
// Contains unit tests for VolunteerAssignmentService.
//
// These tests verify the main volunteer assignment workflow, including:
// - Viewing pending applications.
// - Approving and assigning volunteers.
// - Allowing Coordinator and Admin users to assign volunteers.
// - Preventing Volunteers from reviewing applications.
// - Rejecting applications.
// - Enforcing opportunity capacity.
// - Freeing capacity when assignments are cancelled.
// - Retrieving assignments for a volunteer.


using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    // Tests the business rules provided by VolunteerAssignmentService.
    [TestClass]
    public class VolunteerAssignmentServiceTests
    {
        // Creates a Volunteer user for assignment-related tests.
        private static User CreateVolunteer(
            string username)
        {
            return User.Create(
                username,
                $"{username}@example.com",
                Role.Volunteer);
        }

        // Creates a Coordinator user who is allowed to review
        // applications and manage assignments.
        private static User CreateCoordinator()
        {
            return User.Create(
                "coordinator",
                "coordinator@example.com",
                Role.Coordinator);
        }

        // Creates an Admin user who is also allowed to review
        // applications and manage assignments.
        private static User CreateAdmin()
        {
            return User.Create(
                "admin",
                "admin@example.com",
                Role.Admin);
        }

        // Creates and publishes a volunteer opportunity for tests that
        // require an active opportunity.
        //
        // The number of required volunteers can be changed to test
        // assignment capacity limits.
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

        // Verifies that Pending applications for a specific opportunity
        // can be retrieved by the assignment service.
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

        // Verifies that a Coordinator can approve a Pending application
        // and create an active volunteer assignment.
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

        // Verifies that an Admin can also approve an application
        // and create a volunteer assignment.
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

        // Verifies that a Volunteer user is not authorised to review
        // applications or assign other volunteers.
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

        // Verifies that a Coordinator can reject a Pending application
        // and that rejection does not create a volunteer assignment.
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

        // Verifies that an opportunity cannot receive more active
        // assignments than the number of volunteers it requires.
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

        // Verifies that cancelling an active assignment frees its place,
        // allowing another volunteer to be assigned to the opportunity.
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

        // Verifies that active assignments belonging to a specific
        // volunteer can be retrieved correctly.
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
