using Volunteer_Management_System;

// The purpose of this program is to demo the reporting features in the console: it creates sample opportunities and volunteer requests, then prints each report.

// Set up the services the demo uses.
VolunteerOpportunityService opportunityService = new();
VolunteerRequestService requestService = new();
ReportingService reportingService = new(requestService, opportunityService);

// Create two sample opportunities: a beach cleanup in 3 days and a food bank shift in 5 days.
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

// Lookup of volunteer names, so the output shows names instead of ids.
Dictionary<Guid, string> volunteerNames = new();

// Creates a new volunteer id and remembers the name that goes with it.
Guid RegisterVolunteer(string name)
{
    Guid id = Guid.NewGuid();
    volunteerNames[id] = name;
    return id;
}

// Register three sample volunteers.
Guid alice = RegisterVolunteer("Alice");
Guid ben = RegisterVolunteer("Ben");
Guid chen = RegisterVolunteer("Chen");

// Alice's beach cleanup request is fulfilled with 3 hours logged.
VolunteerRequest aliceBeachRequest =
    requestService.SubmitRequest(alice, beachCleanup.Id);
requestService.FulfillRequest(aliceBeachRequest.Id);
requestService.LogHours(aliceBeachRequest.Id, 3);

// Ben's beach cleanup request is declined.
VolunteerRequest benBeachRequest =
    requestService.SubmitRequest(ben, beachCleanup.Id);
requestService.DeclineRequest(benBeachRequest.Id);

// Chen's beach cleanup request is left pending.
VolunteerRequest chenBeachRequest =
    requestService.SubmitRequest(chen, beachCleanup.Id);

// Alice's food bank request is fulfilled with 4 hours logged.
VolunteerRequest aliceFoodBankRequest =
    requestService.SubmitRequest(alice, foodBankSorting.Id);
requestService.FulfillRequest(aliceFoodBankRequest.Id);
requestService.LogHours(aliceFoodBankRequest.Id, 4);

// Chen's food bank request is fulfilled with 2.5 hours logged.
VolunteerRequest chenFoodBankRequest =
    requestService.SubmitRequest(chen, foodBankSorting.Id);
requestService.FulfillRequest(chenFoodBankRequest.Id);
requestService.LogHours(chenFoodBankRequest.Id, 2.5);

// Looks up a volunteer's name from their id.
string Name(Guid volunteerId) => volunteerNames[volunteerId];

// Print one line per volunteer: request counts by status and total hours.
Console.WriteLine("=== Volunteer Participation Report ===");
foreach (VolunteerParticipationSummary summary in
    reportingService.GetVolunteerParticipationReport())
{
    Console.WriteLine(
        $"{Name(summary.VolunteerId),-8} " +
        $"Total: {summary.TotalRequests}  " +
        $"Fulfilled: {summary.FulfilledRequests}  " +
        $"Pending: {summary.PendingRequests}  " +
        $"Declined: {summary.DeclinedRequests}  " +
        $"Hours: {summary.TotalHoursLogged}");
}

// Print the statistics for each opportunity.
Console.WriteLine();
Console.WriteLine("=== Event Statistics & Volunteer Hours ===");
foreach (VolunteerOpportunity opportunity in
    opportunityService.GetAllOpportunities())
{
    EventStatistics statistics =
        reportingService.GetEventStatistics(opportunity.Id);

    Console.WriteLine(
        $"{statistics.Title,-18} " +
        $"Needed: {statistics.VolunteersNeeded}  " +
        $"Requests: {statistics.RequestsReceived}  " +
        $"Fulfilled: {statistics.FulfilledRequests}  " +
        $"Pending: {statistics.PendingRequests}  " +
        $"Declined: {statistics.DeclinedRequests}  " +
        $"Hours: {statistics.TotalHoursLogged}");
}

// Print every request that is still waiting for a response.
Console.WriteLine();
Console.WriteLine("=== Pending Volunteer Requests ===");
foreach (VolunteerRequest request in reportingService.GetPendingRequests())
{
    VolunteerOpportunity opportunity =
        opportunityService.FindOpportunityById(request.OpportunityId)!;

    Console.WriteLine($"{Name(request.VolunteerId)} -> {opportunity.Title}");
}

// Print every fulfilled request with the hours logged.
Console.WriteLine();
Console.WriteLine("=== Fulfilled Volunteer Requests ===");
foreach (VolunteerRequest request in reportingService.GetFulfilledRequests())
{
    VolunteerOpportunity opportunity =
        opportunityService.FindOpportunityById(request.OpportunityId)!;

    Console.WriteLine(
        $"{Name(request.VolunteerId)} -> {opportunity.Title} " +
        $"({request.HoursLogged} hrs)");
}
