using System;
using System.Collections.Generic;
using System.Linq;

// The purpose of this file is to provide reporting for the Volunteer Management System: participation per volunteer, statistics per event, and lists of pending and fulfilled requests.
namespace Volunteer_Management_System
{
    // One row of the volunteer participation report: a single volunteer's request counts and hours.
    public class VolunteerParticipationSummary
    {
        // The volunteer this summary is for.
        public Guid VolunteerId { get; init; }

        // Total number of requests the volunteer has made.
        public int TotalRequests { get; init; }

        // How many of those were fulfilled.
        public int FulfilledRequests { get; init; }

        // How many are still waiting for a response.
        public int PendingRequests { get; init; }

        // How many were declined.
        public int DeclinedRequests { get; init; }

        // Total hours logged across all of the volunteer's requests.
        public double TotalHoursLogged { get; init; }
    }

    // Statistics for a single volunteer opportunity (event).
    public class EventStatistics
    {
        // The opportunity these statistics are for.
        public Guid OpportunityId { get; init; }

        // The opportunity's title, for display.
        public string Title { get; init; } = string.Empty;

        // How many volunteers the opportunity needs.
        public int VolunteersNeeded { get; init; }

        // Total number of requests made for this opportunity.
        public int RequestsReceived { get; init; }

        // How many of those were fulfilled.
        public int FulfilledRequests { get; init; }

        // How many are still waiting for a response.
        public int PendingRequests { get; init; }

        // How many were declined.
        public int DeclinedRequests { get; init; }

        // Total hours logged by volunteers for this opportunity.
        public double TotalHoursLogged { get; init; }
    }

    // Builds reports from the request and opportunity services. It only reads data and never changes it.
    public class ReportingService
    {
        // The services the reports read their data from.
        private readonly VolunteerRequestService _requestService;
        private readonly VolunteerOpportunityService _opportunityService;

        // Both services are required; passing null throws ArgumentNullException.
        public ReportingService(
            VolunteerRequestService requestService,
            VolunteerOpportunityService opportunityService)
        {
            _requestService = requestService
                ?? throw new ArgumentNullException(nameof(requestService));
            _opportunityService = opportunityService
                ?? throw new ArgumentNullException(nameof(opportunityService));
        }

        // Returns one summary per volunteer who has made at least one request.
        public IReadOnlyList<VolunteerParticipationSummary> GetVolunteerParticipationReport()
        {
            // Group every request by volunteer, then count each status and add up the hours in each group.
            return _requestService
                .GetAllRequests()
                .GroupBy(request => request.VolunteerId)
                .Select(group => new VolunteerParticipationSummary
                {
                    VolunteerId = group.Key,
                    TotalRequests = group.Count(),
                    FulfilledRequests = group.Count(
                        request => request.Status == VolunteerRequestStatus.Fulfilled),
                    PendingRequests = group.Count(
                        request => request.Status == VolunteerRequestStatus.Pending),
                    DeclinedRequests = group.Count(
                        request => request.Status == VolunteerRequestStatus.Declined),
                    TotalHoursLogged = group.Sum(request => request.HoursLogged)
                })
                .ToList();
        }

        // Returns request counts and total hours for one opportunity. Throws KeyNotFoundException if the opportunity doesn't exist.
        public EventStatistics GetEventStatistics(Guid opportunityId)
        {
            VolunteerOpportunity? opportunity =
                _opportunityService.FindOpportunityById(opportunityId);

            if (opportunity == null)
            {
                throw new KeyNotFoundException(
                    "Volunteer opportunity was not found.");
            }

            IReadOnlyList<VolunteerRequest> requests =
                _requestService.GetRequestsForOpportunity(opportunityId);

            return new EventStatistics
            {
                OpportunityId = opportunity.Id,
                Title = opportunity.Title,
                VolunteersNeeded = opportunity.VolunteersNeeded,
                RequestsReceived = requests.Count,
                FulfilledRequests = requests.Count(
                    request => request.Status == VolunteerRequestStatus.Fulfilled),
                PendingRequests = requests.Count(
                    request => request.Status == VolunteerRequestStatus.Pending),
                DeclinedRequests = requests.Count(
                    request => request.Status == VolunteerRequestStatus.Declined),
                TotalHoursLogged = requests.Sum(request => request.HoursLogged)
            };
        }

        // Returns every request that is still waiting for a response.
        public IReadOnlyList<VolunteerRequest> GetPendingRequests()
        {
            return _requestService
                .GetAllRequests()
                .Where(request => request.Status == VolunteerRequestStatus.Pending)
                .ToList();
        }

        // Returns every request that has been fulfilled.
        public IReadOnlyList<VolunteerRequest> GetFulfilledRequests()
        {
            return _requestService
                .GetAllRequests()
                .Where(request => request.Status == VolunteerRequestStatus.Fulfilled)
                .ToList();
        }
    }
}
