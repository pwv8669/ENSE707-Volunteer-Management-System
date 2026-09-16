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

        public DateTime? ReviewedAt { get; private set; }

        public Guid? ReviewedByUserId { get; private set; }

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

        public void Approve(Guid reviewedByUserId)
        {
            ValidateReview(reviewedByUserId);

            Status = VolunteerApplicationStatus.Approved;
            ReviewedByUserId = reviewedByUserId;
            ReviewedAt = DateTime.UtcNow;
        }

        public void Reject(Guid reviewedByUserId)
        {
            ValidateReview(reviewedByUserId);

            Status = VolunteerApplicationStatus.Rejected;
            ReviewedByUserId = reviewedByUserId;
            ReviewedAt = DateTime.UtcNow;
        }

        private void ValidateReview(Guid reviewedByUserId)
        {
            if (reviewedByUserId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Reviewer id is required.",
                    nameof(reviewedByUserId));
            }

            if (Status != VolunteerApplicationStatus.Pending)
            {
                throw new InvalidOperationException(
                    "Only pending applications can be reviewed.");
            }
        }
    }
}