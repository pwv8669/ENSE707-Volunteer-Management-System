using System;

namespace Volunteer_Management_System
{
    public enum OpportunityStatus
    {
        Draft,
        Published,
        Archived
    }

    public class VolunteerOpportunity
    {
        public Guid Id { get; init; }

        public string Title { get; private set; }

        public string Description { get; private set; }

        public string Location { get; private set; }

        public DateTime StartDateTime { get; private set; }

        public DateTime EndDateTime { get; private set; }

        public string RequiredSkills { get; private set; }

        public int VolunteersNeeded { get; private set; }

        public OpportunityStatus Status { get; private set; }

        public DateTime CreatedAt { get; init; }

        private VolunteerOpportunity()
        {
            Title = string.Empty;
            Description = string.Empty;
            Location = string.Empty;
            RequiredSkills = string.Empty;
        }

        public static VolunteerOpportunity Create(
            string title,
            string description,
            string location,
            DateTime startDateTime,
            DateTime endDateTime,
            string requiredSkills,
            int volunteersNeeded)
        {
            return new VolunteerOpportunity
            {
                Id = Guid.NewGuid(),
                Title = title.Trim(),
                Description = description.Trim(),
                Location = location.Trim(),
                StartDateTime = startDateTime,
                EndDateTime = endDateTime,
                RequiredSkills = requiredSkills.Trim(),
                VolunteersNeeded = volunteersNeeded,
                Status = OpportunityStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}