using System;

namespace Volunteer_Management_System
{
    public enum VolunteerRequestStatus
    {
        Pending,
        Fulfilled,
        Declined
    }

    public class VolunteerRequest
    {
        public Guid Id { get; init; }

        public Guid VolunteerId { get; init; }

        public Guid OpportunityId { get; init; }

        public VolunteerRequestStatus Status { get; private set; }

        public DateTime RequestedAt { get; init; }

        public DateTime? RespondedAt { get; private set; }

        public double HoursLogged { get; private set; }

        private VolunteerRequest()
        {
        }

        public static VolunteerRequest Create(Guid volunteerId, Guid opportunityId)
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

            return new VolunteerRequest
            {
                Id = Guid.NewGuid(),
                VolunteerId = volunteerId,
                OpportunityId = opportunityId,
                Status = VolunteerRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };
        }
    }
}
