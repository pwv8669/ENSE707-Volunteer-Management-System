using Volunteer_Management_System;

namespace WebApp.Data
{
    // Persisted Feature 2 opportunity data. This is the source of truth for
    // the WebApp, rather than a list that exists only while the app is running.
    public class VolunteerOpportunityRecord
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public string RequiredSkills { get; set; } = string.Empty;
        public int VolunteersNeeded { get; set; }
        public OpportunityStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
