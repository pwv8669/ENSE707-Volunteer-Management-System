namespace WebApp.Data
{
    // One window of time a volunteer has said they are available, saved in the database.
    public class VolunteerAvailabilitySlot
    {
        public Guid Id { get; set; }

        // The Identity user (volunteer) this slot belongs to.
        public string UserId { get; set; } = string.Empty;

        // Start and end of the window in local (New Zealand) wall-clock time, the
        // same way the volunteer typed it. Stored without a time zone on purpose.
        public DateTime StartsAt { get; set; }

        public DateTime EndsAt { get; set; }

        // Optional short note, e.g. "Can drive" or "Mornings only".
        public string? Note { get; set; }

        // When the slot was created (UTC).
        public DateTime CreatedAt { get; set; }
    }
}
