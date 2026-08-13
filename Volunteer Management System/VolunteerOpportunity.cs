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
            ValidateDetails(
                title,
                description,
                location,
                startDateTime,
                endDateTime,
                requiredSkills,
                volunteersNeeded);

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

        public void UpdateDetails(
            string title,
            string description,
            string location,
            DateTime startDateTime,
            DateTime endDateTime,
            string requiredSkills,
            int volunteersNeeded)
        {
            ValidateDetails(
                title,
                description,
                location,
                startDateTime,
                endDateTime,
                requiredSkills,
                volunteersNeeded);

            Title = title.Trim();
            Description = description.Trim();
            Location = location.Trim();
            StartDateTime = startDateTime;
            EndDateTime = endDateTime;
            RequiredSkills = requiredSkills.Trim();
            VolunteersNeeded = volunteersNeeded;
        }

        private static void ValidateDetails(
            string title,
            string description,
            string location,
            DateTime startDateTime,
            DateTime endDateTime,
            string requiredSkills,
            int volunteersNeeded)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException(
                    "Title is required.",
                    nameof(title));
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException(
                    "Description is required.",
                    nameof(description));
            }

            if (string.IsNullOrWhiteSpace(location))
            {
                throw new ArgumentException(
                    "Location is required.",
                    nameof(location));
            }

            if (startDateTime <= DateTime.UtcNow)
            {
                throw new ArgumentException(
                    "Start date and time must be in the future.",
                    nameof(startDateTime));
            }

            if (endDateTime <= startDateTime)
            {
                throw new ArgumentException(
                    "End date and time must be after the start date and time.",
                    nameof(endDateTime));
            }

            if (string.IsNullOrWhiteSpace(requiredSkills))
            {
                throw new ArgumentException(
                    "Required skills are required.",
                    nameof(requiredSkills));
            }

            if (volunteersNeeded <= 0)
            {
                throw new ArgumentException(
                    "Volunteers needed must be greater than zero.",
                    nameof(volunteersNeeded));
            }
        }
    }
}