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

        public void Fulfill()
        {
            if (Status != VolunteerRequestStatus.Pending)
            {
                throw new InvalidOperationException(
                    "Only pending requests can be fulfilled.");
            }

            Status = VolunteerRequestStatus.Fulfilled;
            RespondedAt = DateTime.UtcNow;
        }

        public void Decline()
        {
            if (Status != VolunteerRequestStatus.Pending)
            {
                throw new InvalidOperationException(
                    "Only pending requests can be declined.");
            }

            Status = VolunteerRequestStatus.Declined;
            RespondedAt = DateTime.UtcNow;
        }

        public void LogHours(double hours)
        {
            if (Status != VolunteerRequestStatus.Fulfilled)
            {
                throw new InvalidOperationException(
                    "Hours can only be logged for fulfilled requests.");
            }

            if (hours <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hours),
                    "Hours must be greater than zero.");
            }

            HoursLogged += hours;
        }
    }
}
