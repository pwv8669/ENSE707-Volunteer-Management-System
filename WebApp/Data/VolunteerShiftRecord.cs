namespace WebApp.Data
{
    // A persisted snapshot of an assigned volunteer shift. Opportunity details
    // are copied here so the volunteer's schedule survives application restarts.
    public class VolunteerShiftRecord
    {
        // Uses the domain assignment ID so the assignment and schedule entry
        // can be matched without maintaining a second identifier.
        public Guid Id { get; set; }

        public string VolunteerId { get; set; } = string.Empty;

        public Guid OpportunityId { get; set; }

        public Guid ApplicationId { get; set; }

        public Guid AssignedByUserId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string RequiredSkills { get; set; } = string.Empty;

        public DateTime StartsAt { get; set; }

        public DateTime EndsAt { get; set; }

        public DateTime AssignedAt { get; set; }

        public VolunteerShiftStatus Status { get; set; }

        public DateTime? CancelledAt { get; set; }
    }

    public enum VolunteerShiftStatus
    {
        Assigned,
        Cancelled
    }
}
