using System;

namespace Volunteer_Management_System
{
    public enum VolunteerApplicationStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class VolunteerApplication
    {
        public Guid Id { get; init; }

        public Guid VolunteerId { get; init; }

        public Guid OpportunityId { get; init; }

        public VolunteerApplicationStatus Status { get; private set; }

        public DateTime AppliedAt { get; init; }

        private VolunteerApplication()
        {
        }

        public static VolunteerApplication Create(
            Guid volunteerId,
            Guid opportunityId)
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

            return new VolunteerApplication
            {
                Id = Guid.NewGuid(),
                VolunteerId = volunteerId,
                OpportunityId = opportunityId,
                Status = VolunteerApplicationStatus.Pending,
                AppliedAt = DateTime.UtcNow
            };
        }
    }
}