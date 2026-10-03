using Volunteer_Management_System;

namespace WebApp.Services
{
    // Demo data for local development only (Program.cs and the profile page only call this in the
    // Development environment). Opportunities and requests live in memory, so without this a fresh
    // run would have nothing to show on the history or reporting pages.
    public static class DevelopmentSampleData
    {
        // Creates a few sample opportunities, once per app run.
        // The domain only allows events that start in the future, so they are all upcoming.
        public static void SeedOpportunities(VolunteerOpportunityService opportunities)
        {
            ArgumentNullException.ThrowIfNull(opportunities);

            if (opportunities.GetAllOpportunities().Count > 0)
            {
                return;
            }

            DateTime today = DateTime.Now.Date;

            CreatePublished(opportunities, "Beach Cleanup", "Help clean up Mission Bay beach.",
                "Mission Bay, Auckland", today.AddDays(2).AddHours(9), 3, "Teamwork", 12);
            CreatePublished(opportunities, "Food Bank Sorting", "Sort and pack donated food parcels.",
                "Auckland City Mission", today.AddDays(5).AddHours(10), 4, "Attention to detail", 8);
            CreatePublished(opportunities, "Community Garden Planting", "Plant seedlings and mulch garden beds.",
                "Grey Lynn Community Garden", today.AddDays(9).AddHours(13), 3, "Gardening", 6);
            CreatePublished(opportunities, "Charity Fun Run Marshal", "Guide runners and keep the course safe.",
                "Auckland Domain", today.AddDays(14).AddHours(7), 5, "Communication", 20);
        }

        // Gives a volunteer a small, varied history: two fulfilled activities with hours,
        // one declined and one still pending. Returns false if they already have history.
        public static bool AddSampleHistory(
            VolunteerRequestService requests,
            VolunteerOpportunityService opportunities,
            Guid volunteerId)
        {
            ArgumentNullException.ThrowIfNull(requests);
            ArgumentNullException.ThrowIfNull(opportunities);

            if (volunteerId == Guid.Empty || requests.GetRequestsForVolunteer(volunteerId).Count > 0)
            {
                return false;
            }

            SeedOpportunities(opportunities);
            IReadOnlyList<VolunteerOpportunity> events = opportunities.GetAllOpportunities();
            if (events.Count < 4)
            {
                return false;
            }

            VolunteerRequest beach = requests.SubmitRequest(volunteerId, events[0].Id);
            requests.FulfillRequest(beach.Id);
            requests.LogHours(beach.Id, 3);

            VolunteerRequest foodBank = requests.SubmitRequest(volunteerId, events[1].Id);
            requests.FulfillRequest(foodBank.Id);
            requests.LogHours(foodBank.Id, 4.5);

            VolunteerRequest garden = requests.SubmitRequest(volunteerId, events[2].Id);
            requests.DeclineRequest(garden.Id);

            requests.SubmitRequest(volunteerId, events[3].Id);

            return true;
        }

        private static void CreatePublished(
            VolunteerOpportunityService opportunities,
            string title,
            string description,
            string location,
            DateTime start,
            int hours,
            string skills,
            int volunteersNeeded)
        {
            VolunteerOpportunity opportunity = opportunities.CreateOpportunity(
                title, description, location, start, start.AddHours(hours), skills, volunteersNeeded);
            opportunities.PublishOpportunity(opportunity.Id);
        }
    }
}
