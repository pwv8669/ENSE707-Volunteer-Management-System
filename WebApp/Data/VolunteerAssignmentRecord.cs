using Volunteer_Management_System;

namespace WebApp.Data
{
    // Persisted Feature 4 allocation. A matching saved shift provides the
    // volunteer-facing schedule and notifications for an active allocation.
    public class VolunteerAssignmentRecord
    {
        public Guid Id { get; set; }
        public string VolunteerId { get; set; } = string.Empty;
        public Guid OpportunityId { get; set; }
        public Guid ApplicationId { get; set; }
        public string AssignedByUserId { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
        public AssignmentStatus Status { get; set; }
        public DateTime? CancelledAt { get; set; }
    }
}
