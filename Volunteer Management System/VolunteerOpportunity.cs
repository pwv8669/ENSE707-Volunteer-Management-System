
// Purpose:
// Represents a volunteer opportunity in the Volunteer Management System.
// This class stores the main details of an opportunity such as the title,
// description, location, date/time, required skills and number of volunteers.
//
// It also manages the lifecycle of an opportunity by allowing it to be
// created, updated, published and archived.


using System;

namespace Volunteer_Management_System
{
    // Defines the possible lifecycle states of a volunteer opportunity.
    // Draft     = created but not yet visible to volunteers.
    // Published = available for volunteers to browse and apply for.
    // Archived  = no longer active or available for new applications.
    public enum OpportunityStatus
    {
        Draft,
        Published,
        Archived
    }

    // Represents one volunteer opportunity/event within the system.
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

        // Private constructor prevents opportunities from being created
        // directly without going through the Create method and validation.
        private VolunteerOpportunity()
        {
            Title = string.Empty;
            Description = string.Empty;
            Location = string.Empty;
            RequiredSkills = string.Empty;
        }

        // Creates a new volunteer opportunity.
        // All details are validated first, and every new opportunity
        // begins with Draft status until it is published.
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

        // Updates the details of an existing volunteer opportunity.
        // The new values must pass the same validation rules used when
        // creating an opportunity.
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

        // Publishes a Draft opportunity so volunteers can browse and
        // apply for it. Only opportunities currently in Draft status
        // are allowed to be published.
        public void Publish()
        {
            if (Status != OpportunityStatus.Draft)
            {
                throw new InvalidOperationException(
                    "Only draft opportunities can be published.");
            }

            Status = OpportunityStatus.Published;
        }

        // Archives an opportunity so it is no longer considered active.
        // An opportunity that is already archived cannot be archived again.
        public void Archive()
        {
            if (Status == OpportunityStatus.Archived)
            {
                throw new InvalidOperationException(
                    "Opportunity is already archived.");
            }

            Status = OpportunityStatus.Archived;
        }

        // Validates all important opportunity information before an
        // opportunity is created or updated.
        //
        // This ensures:
        // - Required text fields are not empty.
        // - The event begins in the future.
        // - The end time is after the start time.
        // - At least one volunteer is required.
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