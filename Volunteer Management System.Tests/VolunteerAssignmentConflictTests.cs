
// Purpose:
// Contains integration-style unit tests for volunteer assignment availability,
// scheduling conflicts and opportunity capacity.
//
// These tests verify that:
// - Available volunteers can be assigned.
// - Unavailable volunteers cannot be assigned.
// - Volunteers cannot receive overlapping assignments.
// - Non-overlapping assignments are allowed.
// - Remaining volunteer capacity is calculated correctly.
//
// This test file supports Feature 4: Volunteer Assignment and Availability,
// while also preparing conflict-management behaviour for scheduling.


using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    // Tests assignment rules involving volunteer availability,
    // scheduling conflicts and remaining capacity.
    [TestClass]
    public class VolunteerAssignmentConflictTests
    {
        // Creates a Volunteer user used throughout the conflict tests.
        private static User CreateVolunteer()
        {
            return User.Create(
                "volunteer",
                "volunteer@example.com",
                Role.Volunteer);
        }

        // Creates a Coordinator user who can approve applications
        // and assign volunteers.
        private static User CreateCoordinator()
        {
            return User.Create(
                "coordinator",
                "coordinator@example.com",
                Role.Coordinator);
        }

        // Creates and publishes an opportunity with the specified
        // start and end times for conflict-related tests.
        private static VolunteerOpportunity CreateOpportunity(
            VolunteerOpportunityService opportunityService,
            string title,
            DateTime start,
            DateTime end)
        {
            VolunteerOpportunity opportunity =
                opportunityService.CreateOpportunity(
                    title,
                    "Volunteer event.",
                    "Auckland",
                    start,
                    end,
                    "Teamwork",
                    5);

            opportunityService.PublishOpportunity(
                opportunity.Id);

            return opportunity;
        }

        // Verifies that a volunteer can be approved and assigned when their
        // availability fully covers the opportunity time.
        [TestMethod]
        public void ApproveAndAssign_WhenVolunteerAvailable_CreatesAssignment()
        {
            VolunteerOpportunityService opportunityService = new();

            DateTime start =
                DateTime.UtcNow.AddDays(7);

            VolunteerOpportunity opportunity =
                CreateOpportunity(
                    opportunityService,
                    "Beach Cleanup",
                    start,
                    start.AddHours(3));

            VolunteerApplicationService applicationService =
                new(opportunityService);

            VolunteerAvailabilityService availabilityService =
                new();

            User volunteer =
                CreateVolunteer();

            availabilityService.AddAvailability(
                volunteer,
                start.AddHours(-1),
                start.AddHours(5));

            VolunteerApplication application =
                applicationService.SubmitApplication(
                    volunteer,
                    opportunity.Id);

            VolunteerAssignmentService assignmentService =
                new(
                    opportunityService,
                    applicationService,
                    availabilityService);

            VolunteerAssignment assignment =
                assignmentService.ApproveAndAssign(
                    CreateCoordinator(),
                    application.Id);

            Assert.AreEqual(
                AssignmentStatus.Assigned,
                assignment.Status);
        }

        // Verifies that a volunteer cannot be assigned when their
        // availability does not cover the opportunity time.
        [TestMethod]
        public void ApproveAndAssign_WhenVolunteerUnavailable_ThrowsException()
        {
            VolunteerOpportunityService opportunityService = new();

            DateTime start =
                DateTime.UtcNow.AddDays(7);

            VolunteerOpportunity opportunity =
                CreateOpportunity(
                    opportunityService,
                    "Beach Cleanup",
                    start,
                    start.AddHours(3));

            VolunteerApplicationService applicationService =
                new(opportunityService);

            VolunteerAvailabilityService availabilityService =
                new();

            User volunteer =
                CreateVolunteer();

            availabilityService.AddAvailability(
                volunteer,
                start.AddHours(5),
                start.AddHours(8));

            VolunteerApplication application =
                applicationService.SubmitApplication(
                    volunteer,
                    opportunity.Id);

            VolunteerAssignmentService assignmentService =
                new(
                    opportunityService,
                    applicationService,
                    availabilityService);

            InvalidOperationException exception =
                Assert.ThrowsExactly<InvalidOperationException>(() =>
                    assignmentService.ApproveAndAssign(
                        CreateCoordinator(),
                        application.Id));

            StringAssert.Contains(
                exception.Message,
                "not available");
        }

        // Verifies that a volunteer cannot be assigned to a second
        // opportunity when its scheduled time overlaps an existing
        // active assignment.
        [TestMethod]
        public void ApproveAndAssign_WithOverlappingAssignment_ThrowsException()
        {
            VolunteerOpportunityService opportunityService = new();

            DateTime start =
                DateTime.UtcNow.AddDays(7);

            VolunteerOpportunity firstOpportunity =
                CreateOpportunity(
                    opportunityService,
                    "Beach Cleanup",
                    start,
                    start.AddHours(3));

            VolunteerOpportunity secondOpportunity =
                CreateOpportunity(
                    opportunityService,
                    "Food Drive",
                    start.AddHours(2),
                    start.AddHours(5));

            VolunteerApplicationService applicationService =
                new(opportunityService);

            VolunteerAvailabilityService availabilityService =
                new();

            User volunteer =
                CreateVolunteer();

            availabilityService.AddAvailability(
                volunteer,
                start.AddHours(-1),
                start.AddHours(7));

            VolunteerApplication firstApplication =
                applicationService.SubmitApplication(
                    volunteer,
                    firstOpportunity.Id);

            VolunteerApplication secondApplication =
                applicationService.SubmitApplication(
                    volunteer,
                    secondOpportunity.Id);

            VolunteerAssignmentService assignmentService =
                new(
                    opportunityService,
                    applicationService,
                    availabilityService);

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
                "overlapping");
        }

        // Verifies that the same volunteer can be assigned to multiple
        // opportunities when their scheduled times do not overlap.
        [TestMethod]
        public void ApproveAndAssign_WithNonOverlappingAssignments_AllowsBoth()
        {
            VolunteerOpportunityService opportunityService = new();

            DateTime start =
                DateTime.UtcNow.AddDays(7);

            VolunteerOpportunity firstOpportunity =
                CreateOpportunity(
                    opportunityService,
                    "Beach Cleanup",
                    start,
                    start.AddHours(2));

            VolunteerOpportunity secondOpportunity =
                CreateOpportunity(
                    opportunityService,
                    "Food Drive",
                    start.AddHours(3),
                    start.AddHours(5));

            VolunteerApplicationService applicationService =
                new(opportunityService);

            VolunteerAvailabilityService availabilityService =
                new();

            User volunteer =
                CreateVolunteer();

            availabilityService.AddAvailability(
                volunteer,
                start.AddHours(-1),
                start.AddHours(7));

            VolunteerApplication firstApplication =
                applicationService.SubmitApplication(
                    volunteer,
                    firstOpportunity.Id);

            VolunteerApplication secondApplication =
                applicationService.SubmitApplication(
                    volunteer,
                    secondOpportunity.Id);

            VolunteerAssignmentService assignmentService =
                new(
                    opportunityService,
                    applicationService,
                    availabilityService);

            User coordinator =
                CreateCoordinator();

            assignmentService.ApproveAndAssign(
                coordinator,
                firstApplication.Id);

            VolunteerAssignment secondAssignment =
                assignmentService.ApproveAndAssign(
                    coordinator,
                    secondApplication.Id);

            Assert.AreEqual(
                AssignmentStatus.Assigned,
                secondAssignment.Status);

            Assert.HasCount(
                2,
                assignmentService.GetAssignmentsForVolunteer(
                    volunteer.Id));
        }

        // Verifies that remaining opportunity capacity is calculated from
        // the number of active volunteer assignments.
        [TestMethod]
        public void GetRemainingCapacity_AfterAssignment_ReturnsCorrectNumber()
        {
            VolunteerOpportunityService opportunityService = new();

            DateTime start =
                DateTime.UtcNow.AddDays(7);

            VolunteerOpportunity opportunity =
                opportunityService.CreateOpportunity(
                    "Community Event",
                    "Help with the event.",
                    "Auckland",
                    start,
                    start.AddHours(3),
                    "Teamwork",
                    3);

            opportunityService.PublishOpportunity(
                opportunity.Id);

            VolunteerApplicationService applicationService =
                new(opportunityService);

            User volunteer =
                CreateVolunteer();

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

            int remaining =
                assignmentService.GetRemainingCapacity(
                    opportunity.Id);

            Assert.AreEqual(
                2,
                remaining);
        }
    }
}
