
// Purpose:
// Provides the main management operations for volunteer opportunities.
//
// This service is responsible for creating, retrieving, updating, publishing,
// archiving and deleting volunteer opportunities. It also provides a way to
// retrieve only published opportunities for volunteers to browse.


using System;
using System.Collections.Generic;
using System.Linq;

namespace Volunteer_Management_System
{
    // Handles the management and retrieval of volunteer opportunities.
    public class VolunteerOpportunityService
    {
        // Stores the volunteer opportunities currently managed by the service.
        private readonly List<VolunteerOpportunity> _opportunities = new();

        // Creates a new volunteer opportunity using the validation rules
        // inside VolunteerOpportunity.Create, then stores it in the service.
        public VolunteerOpportunity CreateOpportunity(
            string title,
            string description,
            string location,
            DateTime startDateTime,
            DateTime endDateTime,
            string requiredSkills,
            int volunteersNeeded)
        {
            VolunteerOpportunity opportunity =
                VolunteerOpportunity.Create(
                    title,
                    description,
                    location,
                    startDateTime,
                    endDateTime,
                    requiredSkills,
                    volunteersNeeded);

            _opportunities.Add(opportunity);

            return opportunity;
        }

        // Returns all volunteer opportunities, including Draft,
        // Published and Archived opportunities.
        public IReadOnlyList<VolunteerOpportunity> GetAllOpportunities()
        {
            return _opportunities.AsReadOnly();
        }

        // Returns only Published opportunities.
        // This is used when volunteers browse opportunities that are
        // currently available for applications.
        public IReadOnlyList<VolunteerOpportunity> GetPublishedOpportunities()
        {
            return _opportunities
                .Where(opportunity =>
                    opportunity.Status == OpportunityStatus.Published)
                .ToList()
                .AsReadOnly();
        }

        // Searches for an opportunity using its unique ID.
        // Returns null when no matching opportunity exists.
        public VolunteerOpportunity? FindOpportunityById(Guid opportunityId)
        {
            return _opportunities.FirstOrDefault(
                opportunity => opportunity.Id == opportunityId);
        }

        // Updates the details of an existing volunteer opportunity.
        // The opportunity must exist before its details can be changed.
        public void UpdateOpportunity(
            Guid opportunityId,
            string title,
            string description,
            string location,
            DateTime startDateTime,
            DateTime endDateTime,
            string requiredSkills,
            int volunteersNeeded)
        {
            VolunteerOpportunity opportunity =
                GetOpportunityOrThrow(opportunityId);

            opportunity.UpdateDetails(
                title,
                description,
                location,
                startDateTime,
                endDateTime,
                requiredSkills,
                volunteersNeeded);
        }

        // Publishes an existing Draft opportunity so volunteers can
        // browse and apply for it.
        public void PublishOpportunity(Guid opportunityId)
        {
            VolunteerOpportunity opportunity =
                GetOpportunityOrThrow(opportunityId);

            opportunity.Publish();
        }

        // Archives an existing opportunity so it is no longer active
        // or shown as an available opportunity.
        public void ArchiveOpportunity(Guid opportunityId)
        {
            VolunteerOpportunity opportunity =
                GetOpportunityOrThrow(opportunityId);

            opportunity.Archive();
        }

        // Removes an opportunity from the service.
        // Returns false if the opportunity cannot be found.
        public bool DeleteOpportunity(Guid opportunityId)
        {
            VolunteerOpportunity? opportunity =
                FindOpportunityById(opportunityId);

            if (opportunity == null)
            {
                return false;
            }

            return _opportunities.Remove(opportunity);
        }

        // Retrieves an opportunity by ID for operations that require
        // the opportunity to exist.
        //
        // A KeyNotFoundException is thrown when the ID does not match
        // any existing volunteer opportunity.
        private VolunteerOpportunity GetOpportunityOrThrow(
            Guid opportunityId)
        {
            VolunteerOpportunity? opportunity =
                FindOpportunityById(opportunityId);

            if (opportunity == null)
            {
                throw new KeyNotFoundException(
                    "Volunteer opportunity was not found.");
            }

            return opportunity;
        }
    }
}