namespace WebApp.Data
{
    // An in-app notification created when a volunteer shift is assigned or
    // cancelled. Notifications remain available after the WebApp restarts.
    public class VolunteerShiftNotificationRecord
    {
        public Guid Id { get; set; }

        public string VolunteerId { get; set; } = string.Empty;

        public Guid ShiftId { get; set; }

        public VolunteerShiftNotificationType Type { get; set; }

        public string OpportunityTitle { get; set; } = string.Empty;

        public DateTime ShiftStartsAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsRead { get; set; }

        public VolunteerShiftRecord Shift { get; set; } = null!;
    }

    public enum VolunteerShiftNotificationType
    {
        Assigned,
        Cancelled
    }
}
