
// Purpose:
// Represents the assignment of a volunteer to a volunteer opportunity.
//
// The class stores which volunteer was assigned, which opportunity they were
// assigned to, the application that resulted in the assignment, who created
// the assignment, and when it was created.
//
// It also manages the current assignment status and supports cancellation.


using System;

namespace Volunteer_Management_System
{
    // Defines the possible states of a volunteer assignment.
    // Assigned  = the volunteer is currently allocated to the opportunity.
    // Cancelled = the assignment is no longer active.
    public enum AssignmentStatus
    {
        Assigned,
        Cancelled
    }

    // Represents one confirmed volunteer assignment to an opportunity.
    public class VolunteerAssignment
    {
        public Guid Id { get; init; }

        public Guid VolunteerId { get; init; }

        public Guid OpportunityId { get; init; }

        public Guid ApplicationId { get; init; }

        public Guid AssignedByUserId { get; init; }

        public DateTime AssignedAt { get; init; }

        public AssignmentStatus Status { get; private set; }

        // Private constructor ensures assignments are created through
        // the Create method so all required IDs are validated first.
        private VolunteerAssignment()
        {
        }

        // Creates a new volunteer assignment.
        //
        // The method validates the volunteer, opportunity, application and
        // assigning user IDs before creating the assignment.
        // New assignments begin with Assigned status.
        public static VolunteerAssignment Create(
            Guid volunteerId,
            Guid opportunityId,
            Guid applicationId,
            Guid assignedByUserId)
        {
            if (volunteerId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Volunteer id is required.",
                    nameof(volunteerId));
            }

            if (opportunityId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Opportunity id is required.",
                    nameof(opportunityId));
            }

            if (applicationId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Application id is required.",
                    nameof(applicationId));
            }

            if (assignedByUserId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Assigned-by user id is required.",
                    nameof(assignedByUserId));
            }

            return new VolunteerAssignment
            {
                Id = Guid.NewGuid(),
                VolunteerId = volunteerId,
                OpportunityId = opportunityId,
                ApplicationId = applicationId,
                AssignedByUserId = assignedByUserId,
                AssignedAt = DateTime.UtcNow,
                Status = AssignmentStatus.Assigned
            };
        }

        // Cancels an active assignment.
        // An assignment that has already been cancelled cannot be
        // cancelled again.
        public void Cancel()
        {
            if (Status == AssignmentStatus.Cancelled)
            {
                throw new InvalidOperationException(
                    "Assignment is already cancelled.");
            }

            Status = AssignmentStatus.Cancelled;
        }
    }
}