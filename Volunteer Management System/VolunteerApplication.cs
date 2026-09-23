
// Purpose:
// Represents an application submitted by a volunteer for a volunteer
// opportunity.
//
// The class stores which volunteer applied, which opportunity they applied
// for, the current application status, and review information.
//
// It also manages application status changes such as approval and rejection.


using System;

namespace Volunteer_Management_System
{
    // Defines the possible states of a volunteer application.
    // Pending  = waiting for review.
    // Approved = accepted by a coordinator or administrator.
    // Rejected = declined during the review process.
    public enum VolunteerApplicationStatus
    {
        Pending,
        Approved,
        Rejected
    }

    // Represents a volunteer's application for one volunteer opportunity.
    public class VolunteerApplication
    {
        public Guid Id { get; init; }

        public Guid VolunteerId { get; init; }

        public Guid OpportunityId { get; init; }

        public VolunteerApplicationStatus Status { get; private set; }

        public DateTime AppliedAt { get; init; }

        public DateTime? ReviewedAt { get; private set; }

        public Guid? ReviewedByUserId { get; private set; }

        // Private constructor ensures applications are created through
        // the Create method so required IDs are validated first.
        private VolunteerApplication()
        {
        }

        // Creates a new volunteer application.
        // A newly submitted application always starts with Pending status.
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

        // Approves a Pending application and records who reviewed it
        // and when the review occurred.
        public void Approve(Guid reviewedByUserId)
        {
            ValidateReview(reviewedByUserId);

            Status = VolunteerApplicationStatus.Approved;
            ReviewedByUserId = reviewedByUserId;
            ReviewedAt = DateTime.UtcNow;
        }

        // Rejects a Pending application and records who reviewed it
        // and when the review occurred.
        public void Reject(Guid reviewedByUserId)
        {
            ValidateReview(reviewedByUserId);

            Status = VolunteerApplicationStatus.Rejected;
            ReviewedByUserId = reviewedByUserId;
            ReviewedAt = DateTime.UtcNow;
        }

        // Validates an application review before approval or rejection.
        // Only Pending applications can be reviewed, and a valid reviewer
        // ID must be supplied.
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