using Volunteer_Management_System;

namespace WebApplication;

public static class SampleDataSeeder
{
    public static Dictionary<Guid, string> Seed(
        VolunteerOpportunityService opportunityService,
        VolunteerRequestService requestService)
    {
        DateTime beachCleanupStart = DateTime.UtcNow.AddDays(3);
        VolunteerOpportunity beachCleanup = opportunityService.CreateOpportunity(
            "Beach Cleanup",
            "Help clean up Mission Bay beach.",
            "Mission Bay",
            beachCleanupStart,
            beachCleanupStart.AddHours(3),
            "Teamwork",
            5);

        DateTime foodBankStart = DateTime.UtcNow.AddDays(5);
        VolunteerOpportunity foodBankSorting = opportunityService.CreateOpportunity(
            "Food Bank Sorting",
            "Sort and pack donated food items.",
            "Auckland City Mission",
            foodBankStart,
            foodBankStart.AddHours(4),
            "Attention to detail",
            8);

        Guid alice = Guid.NewGuid();
        Guid ben = Guid.NewGuid();
        Guid chen = Guid.NewGuid();

        Dictionary<Guid, string> volunteerNames = new()
        {
            [alice] = "Alice",
            [ben] = "Ben",
            [chen] = "Chen"
        };

        VolunteerRequest aliceBeachRequest =
            requestService.SubmitRequest(alice, beachCleanup.Id);
        requestService.FulfillRequest(aliceBeachRequest.Id);
        requestService.LogHours(aliceBeachRequest.Id, 3);

        VolunteerRequest benBeachRequest =
            requestService.SubmitRequest(ben, beachCleanup.Id);
        requestService.DeclineRequest(benBeachRequest.Id);

        requestService.SubmitRequest(chen, beachCleanup.Id);

        VolunteerRequest aliceFoodBankRequest =
            requestService.SubmitRequest(alice, foodBankSorting.Id);
        requestService.FulfillRequest(aliceFoodBankRequest.Id);
        requestService.LogHours(aliceFoodBankRequest.Id, 4);

        VolunteerRequest chenFoodBankRequest =
            requestService.SubmitRequest(chen, foodBankSorting.Id);
        requestService.FulfillRequest(chenFoodBankRequest.Id);
        requestService.LogHours(chenFoodBankRequest.Id, 2.5);

        return volunteerNames;
    }
}
