using System;

// The purpose of this class is to represent a volunteer's request to help at a volunteer opportunity, including its status and the hours logged against it.
namespace Volunteer_Management_System
{
    // The possible states of a request. Every request starts as Pending and can move once to either Fulfilled or Declined.
    public enum VolunteerRequestStatus
    {
        Pending,
        Fulfilled,
        Declined
    }

    // Represents one request from a volunteer to take part in an opportunity.
    public class VolunteerRequest
    {
        // Unique id for this request.
        public Guid Id { get; init; }

        // The volunteer who made the request.
        public Guid VolunteerId { get; init; }

        // The opportunity the request is for.
        public Guid OpportunityId { get; init; }

        // Current state of the request (starts as Pending).
        public VolunteerRequestStatus Status { get; private set; }

        // When the request was made (UTC).
        public DateTime RequestedAt { get; init; }

        // When the request was fulfilled or declined (UTC); null while it is still pending.
        public DateTime? RespondedAt { get; private set; }

        // Running total of hours the volunteer has logged for this request.
        public double HoursLogged { get; private set; }

        // Private constructor so new requests have to be created through Create().
        private VolunteerRequest()
        {
        }

        // Factory for creating a new pending request. Both ids are required.
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

        // Rebuilds a request that already has a known state, for example one worked out from a saved application.
        // The state has to make sense: a pending request has no response time and no hours,
        // a fulfilled or declined request has a response time, and only a fulfilled request can have hours.
        public static VolunteerRequest Restore(
            Guid id,
            Guid volunteerId,
            Guid opportunityId,
            VolunteerRequestStatus status,
            DateTime requestedAt,
            DateTime? respondedAt,
            double hoursLogged)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(
                    "Request id is required.",
                    nameof(id));
            }

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

            if (double.IsNaN(hoursLogged) || hoursLogged < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hoursLogged),
                    "Hours cannot be negative.");
            }

            if (status == VolunteerRequestStatus.Pending &&
                (respondedAt != null || hoursLogged != 0))
            {
                throw new ArgumentException(
                    "A pending request cannot have a response time or hours.",
                    nameof(status));
            }

            if (status != VolunteerRequestStatus.Pending && respondedAt == null)
            {
                throw new ArgumentException(
                    "A fulfilled or declined request needs a response time.",
                    nameof(respondedAt));
            }

            if (status == VolunteerRequestStatus.Declined && hoursLogged != 0)
            {
                throw new ArgumentException(
                    "A declined request cannot have hours logged.",
                    nameof(hoursLogged));
            }

            return new VolunteerRequest
            {
                Id = id,
                VolunteerId = volunteerId,
                OpportunityId = opportunityId,
                Status = status,
                RequestedAt = requestedAt,
                RespondedAt = respondedAt,
                HoursLogged = hoursLogged
            };
        }

        // Marks a pending request as fulfilled and records when that happened.
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

        // Marks a pending request as declined and records when that happened.
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

        // Adds hours to the running total. Only allowed on fulfilled requests, and the hours must be greater than zero.
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
