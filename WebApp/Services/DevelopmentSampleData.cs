using Volunteer_Management_System;

namespace WebApp.Services
{
    // Demo data for local development only (Program.cs only calls this in the Development environment).
    // Opportunities live in memory, so without this a fresh run would have no events to apply for.
    // Applications, assignments and attendance are not faked here: the history and reporting pages show
    // whatever really happens when you apply, approve and record attendance.
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
