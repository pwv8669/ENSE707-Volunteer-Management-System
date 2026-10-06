// Purpose:
// Represents an in-app notification sent to a volunteer when one of their
// shift assignments is created or cancelled.

using System;

namespace Volunteer_Management_System
{
    // Identifies the assignment change described by a notification.
    public enum ShiftNotificationType
    {
        Assigned,
        Cancelled
    }

    // Stores the information a volunteer needs to understand a shift change.
    public class VolunteerShiftNotification
    {
        public Guid Id { get; init; }

        public Guid VolunteerId { get; init; }

        public Guid AssignmentId { get; init; }

        public ShiftNotificationType Type { get; init; }

        public string OpportunityTitle { get; init; } = string.Empty;

        public DateTime ShiftStartsAt { get; init; }

        public DateTime CreatedAt { get; init; }

        public bool IsRead { get; private set; }

        // Creates a new unread notification for an assignment change.
        public static VolunteerShiftNotification Create(
            VolunteerAssignment assignment,
            VolunteerOpportunity opportunity,
            ShiftNotificationType type)
        {
            ArgumentNullException.ThrowIfNull(assignment);
            ArgumentNullException.ThrowIfNull(opportunity);

            return new VolunteerShiftNotification
            {
                Id = Guid.NewGuid(),
                VolunteerId = assignment.VolunteerId,
                AssignmentId = assignment.Id,
                Type = type,
                OpportunityTitle = opportunity.Title,
                ShiftStartsAt = opportunity.StartDateTime,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
        }

        // Marks the notification as seen by its volunteer.
        public void MarkAsRead()
        {
            IsRead = true;
        }
    }
}
