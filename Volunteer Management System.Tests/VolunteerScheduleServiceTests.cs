// Purpose:
// Verifies Feature 5 schedule display data and in-app shift notifications.
// Scheduling conflict behaviour is covered by VolunteerAssignmentConflictTests.

using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class VolunteerScheduleServiceTests
    {
        // Active assignments are joined to their opportunity details and
        // returned in the order the volunteer will attend them.
        [TestMethod]
        public void GetSchedule_WithMultipleAssignments_ReturnsChronologicalDetails()
        {
            VolunteerOpportunityService opportunities = new();
            DateTime start = DateTime.Now.AddDays(7);
            VolunteerOpportunity later = CreatePublishedOpportunity(
                opportunities,
                "Food Drive",
                start.AddHours(4),
                start.AddHours(6));
            VolunteerOpportunity earlier = CreatePublishedOpportunity(
                opportunities,
                "Beach Cleanup",
                start,
                start.AddHours(2));
            VolunteerApplicationService applications = new(opportunities);
            VolunteerAvailabilityService availability = new();
            VolunteerShiftNotificationService notifications = new();
            VolunteerAssignmentService assignments = new(
                opportunities,
                applications,
                availability,
                notifications);
            VolunteerScheduleService schedule = new(assignments, opportunities);
            User volunteer = CreateVolunteer("schedule-volunteer");
            User coordinator = CreateCoordinator();

            availability.AddAvailability(
                volunteer,
                start.AddHours(-1),
                start.AddHours(7));
            VolunteerApplication laterApplication =
                applications.SubmitApplication(volunteer, later.Id);
            VolunteerApplication earlierApplication =
                applications.SubmitApplication(volunteer, earlier.Id);
            assignments.ApproveAndAssign(coordinator, laterApplication.Id);
            assignments.ApproveAndAssign(coordinator, earlierApplication.Id);

            IReadOnlyList<VolunteerScheduledShift> result =
                schedule.GetSchedule(volunteer.Id);

            Assert.HasCount(2, result);
            Assert.AreEqual("Beach Cleanup", result[0].Title);
            Assert.AreEqual("Auckland", result[0].Location);
            Assert.AreEqual("Food Drive", result[1].Title);
        }

        // Approving an application creates an unread notification for the
        // volunteer whose shift was assigned.
        [TestMethod]
        public void ApproveAndAssign_WithNotifications_CreatesUnreadNotification()
        {
            AssignmentScenario scenario = CreateAssignmentScenario();

            VolunteerAssignment assignment =
                scenario.Assignments.ApproveAndAssign(
                    scenario.Coordinator,
                    scenario.Application.Id);

            VolunteerShiftNotification notification =
                scenario.Notifications
                    .GetUnreadNotificationsForVolunteer(
                        scenario.Volunteer.Id)
                    .Single();

            Assert.AreEqual(assignment.Id, notification.AssignmentId);
            Assert.AreEqual(
                ShiftNotificationType.Assigned,
                notification.Type);
            Assert.AreEqual(
                scenario.Opportunity.Title,
                notification.OpportunityTitle);
        }

        // Cancelling a shift removes it from the active schedule and informs
        // the affected volunteer of the cancellation.
        [TestMethod]
        public void CancelAssignment_WithNotifications_RemovesShiftAndNotifiesVolunteer()
        {
            AssignmentScenario scenario = CreateAssignmentScenario();
            VolunteerAssignment assignment =
                scenario.Assignments.ApproveAndAssign(
                    scenario.Coordinator,
                    scenario.Application.Id);
            VolunteerScheduleService schedule = new(
                scenario.Assignments,
                scenario.Opportunities);

            scenario.Assignments.CancelAssignment(
                scenario.Coordinator,
                assignment.Id);

            Assert.HasCount(
                0,
                schedule.GetSchedule(scenario.Volunteer.Id));
            Assert.IsTrue(
                scenario.Notifications
                    .GetNotificationsForVolunteer(
                        scenario.Volunteer.Id)
                    .Any(notification =>
                        notification.Type ==
                            ShiftNotificationType.Cancelled));
        }

        // Reading one volunteer's notifications must not change another
        // volunteer's unread notifications.
        [TestMethod]
        public void MarkAllAsRead_OnlyUpdatesSelectedVolunteer()
        {
            VolunteerOpportunityService opportunities = new();
            DateTime start = DateTime.Now.AddDays(7);
            VolunteerOpportunity opportunity = CreatePublishedOpportunity(
                opportunities,
                "Community Event",
                start,
                start.AddHours(2));
            VolunteerShiftNotificationService notifications = new();
            Guid firstVolunteerId = Guid.NewGuid();
            Guid secondVolunteerId = Guid.NewGuid();

            notifications.NotifyAssigned(
                VolunteerAssignment.Create(
                    firstVolunteerId,
                    opportunity.Id,
                    Guid.NewGuid(),
                    Guid.NewGuid()),
                opportunity);
            notifications.NotifyAssigned(
                VolunteerAssignment.Create(
                    secondVolunteerId,
                    opportunity.Id,
                    Guid.NewGuid(),
                    Guid.NewGuid()),
                opportunity);

            notifications.MarkAllAsRead(firstVolunteerId);

            Assert.HasCount(
                0,
                notifications.GetUnreadNotificationsForVolunteer(
                    firstVolunteerId));
            Assert.HasCount(
                1,
                notifications.GetUnreadNotificationsForVolunteer(
                    secondVolunteerId));
        }

        private static AssignmentScenario CreateAssignmentScenario()
        {
            VolunteerOpportunityService opportunities = new();
            DateTime start = DateTime.Now.AddDays(7);
            VolunteerOpportunity opportunity = CreatePublishedOpportunity(
                opportunities,
                "Beach Cleanup",
                start,
                start.AddHours(3));
            VolunteerApplicationService applications = new(opportunities);
            VolunteerAvailabilityService availability = new();
            VolunteerShiftNotificationService notifications = new();
            User volunteer = CreateVolunteer("notified-volunteer");
            User coordinator = CreateCoordinator();

            availability.AddAvailability(
                volunteer,
                start.AddHours(-1),
                start.AddHours(4));
            VolunteerApplication application =
                applications.SubmitApplication(
                    volunteer,
                    opportunity.Id);
            VolunteerAssignmentService assignments = new(
                opportunities,
                applications,
                availability,
                notifications);

            return new AssignmentScenario(
                opportunities,
                assignments,
                notifications,
                opportunity,
                application,
                volunteer,
                coordinator);
        }

        private static VolunteerOpportunity CreatePublishedOpportunity(
            VolunteerOpportunityService opportunities,
            string title,
            DateTime startsAt,
            DateTime endsAt)
        {
            VolunteerOpportunity opportunity =
                opportunities.CreateOpportunity(
                    title,
                    "Help at this volunteer event.",
                    "Auckland",
                    startsAt,
                    endsAt,
                    "Teamwork",
                    5);
            opportunities.PublishOpportunity(opportunity.Id);
            return opportunity;
        }

        private static User CreateVolunteer(string username) =>
            User.Create(
                username,
                $"{username}@example.com",
                Role.Volunteer);

        private static User CreateCoordinator() =>
            User.Create(
                "schedule-coordinator",
                "schedule-coordinator@example.com",
                Role.Coordinator);

        private sealed record AssignmentScenario(
            VolunteerOpportunityService Opportunities,
            VolunteerAssignmentService Assignments,
            VolunteerShiftNotificationService Notifications,
            VolunteerOpportunity Opportunity,
            VolunteerApplication Application,
            User Volunteer,
            User Coordinator);
    }
}
