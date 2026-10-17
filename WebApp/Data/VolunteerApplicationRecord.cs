using Volunteer_Management_System;

namespace WebApp.Data
{
    // Persisted Feature 3 application, including its review result.
    public class VolunteerApplicationRecord
    {
        public Guid Id { get; set; }
        public string VolunteerId { get; set; } = string.Empty;
        public Guid OpportunityId { get; set; }
        public VolunteerApplicationStatus Status { get; set; }
        public DateTime AppliedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewedByUserId { get; set; }
    }
}
