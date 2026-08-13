using System;
using System.Collections.Generic;
using System.Linq;

namespace Volunteer_Management_System
{
    public class VolunteerOpportunityService
    {
        private readonly List<VolunteerOpportunity> _opportunities = new();

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

        public IReadOnlyList<VolunteerOpportunity> GetAllOpportunities()
        {
            return _opportunities.AsReadOnly();
        }

        public VolunteerOpportunity? FindOpportunityById(Guid opportunityId)
        {
            return _opportunities.FirstOrDefault(
                opportunity => opportunity.Id == opportunityId);
        }

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
            VolunteerOpportunity? opportunity =
                FindOpportunityById(opportunityId);

            if (opportunity == null)
            {
                throw new KeyNotFoundException(
                    "Volunteer opportunity was not found.");
            }

            opportunity.UpdateDetails(
                title,
                description,
                location,
                startDateTime,
                endDateTime,
                requiredSkills,
                volunteersNeeded);
        }
    }
}