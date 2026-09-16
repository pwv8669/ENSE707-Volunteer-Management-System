using System;

namespace Volunteer_Management_System
{
    public enum AssignmentStatus
    {
        Assigned,
        Cancelled
    }

    public class VolunteerAssignment
    {
        public Guid Id { get; init; }

        public Guid VolunteerId { get; init; }

        public Guid OpportunityId { get; init; }

        public Guid ApplicationId { get; init; }

        public Guid AssignedByUserId { get; init; }

        public DateTime AssignedAt { get; init; }

        public AssignmentStatus Status { get; private set; }

        private VolunteerAssignment()
        {
        }

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