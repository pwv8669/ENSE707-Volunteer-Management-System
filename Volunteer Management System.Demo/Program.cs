using Volunteer_Management_System;

VolunteerOpportunityService opportunityService = new();
VolunteerRequestService requestService = new();
ReportingService reportingService = new(requestService, opportunityService);

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

Dictionary<Guid, string> volunteerNames = new();

Guid RegisterVolunteer(string name)
{
    Guid id = Guid.NewGuid();
    volunteerNames[id] = name;
    return id;
}

Guid alice = RegisterVolunteer("Alice");
Guid ben = RegisterVolunteer("Ben");
Guid chen = RegisterVolunteer("Chen");

VolunteerRequest aliceBeachRequest =
    requestService.SubmitRequest(alice, beachCleanup.Id);
requestService.FulfillRequest(aliceBeachRequest.Id);
requestService.LogHours(aliceBeachRequest.Id, 3);

VolunteerRequest benBeachRequest =
    requestService.SubmitRequest(ben, beachCleanup.Id);
requestService.DeclineRequest(benBeachRequest.Id);

VolunteerRequest chenBeachRequest =
    requestService.SubmitRequest(chen, beachCleanup.Id);

VolunteerRequest aliceFoodBankRequest =
    requestService.SubmitRequest(alice, foodBankSorting.Id);
requestService.FulfillRequest(aliceFoodBankRequest.Id);
requestService.LogHours(aliceFoodBankRequest.Id, 4);

VolunteerRequest chenFoodBankRequest =
    requestService.SubmitRequest(chen, foodBankSorting.Id);
requestService.FulfillRequest(chenFoodBankRequest.Id);
requestService.LogHours(chenFoodBankRequest.Id, 2.5);

string Name(Guid volunteerId) => volunteerNames[volunteerId];

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

Console.WriteLine();
Console.WriteLine("=== Pending Volunteer Requests ===");
foreach (VolunteerRequest request in reportingService.GetPendingRequests())
{
    VolunteerOpportunity opportunity =
        opportunityService.FindOpportunityById(request.OpportunityId)!;

    Console.WriteLine($"{Name(request.VolunteerId)} -> {opportunity.Title}");
}

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
