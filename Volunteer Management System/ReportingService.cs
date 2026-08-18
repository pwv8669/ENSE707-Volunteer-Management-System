using System;
using System.Collections.Generic;
using System.Linq;

namespace Volunteer_Management_System
{
    public class VolunteerParticipationSummary
    {
        public Guid VolunteerId { get; init; }

        public int TotalRequests { get; init; }

        public int FulfilledRequests { get; init; }

        public int PendingRequests { get; init; }

        public int DeclinedRequests { get; init; }

        public double TotalHoursLogged { get; init; }
    }

    public class EventStatistics
    {
        public Guid OpportunityId { get; init; }

        public string Title { get; init; } = string.Empty;

        public int VolunteersNeeded { get; init; }

        public int RequestsReceived { get; init; }

        public int FulfilledRequests { get; init; }

        public int PendingRequests { get; init; }

        public int DeclinedRequests { get; init; }

        public double TotalHoursLogged { get; init; }
    }

    public class ReportingService
    {
        private readonly VolunteerRequestService _requestService;
        private readonly VolunteerOpportunityService _opportunityService;

        public ReportingService(
            VolunteerRequestService requestService,
            VolunteerOpportunityService opportunityService)
        {
            _requestService = requestService
                ?? throw new ArgumentNullException(nameof(requestService));
            _opportunityService = opportunityService
                ?? throw new ArgumentNullException(nameof(opportunityService));
        }

        public IReadOnlyList<VolunteerParticipationSummary> GetVolunteerParticipationReport()
        {
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

        public IReadOnlyList<VolunteerRequest> GetPendingRequests()
        {
            return _requestService
                .GetAllRequests()
                .Where(request => request.Status == VolunteerRequestStatus.Pending)
                .ToList();
        }

        public IReadOnlyList<VolunteerRequest> GetFulfilledRequests()
        {
            return _requestService
                .GetAllRequests()
                .Where(request => request.Status == VolunteerRequestStatus.Fulfilled)
                .ToList();
        }
    }
}
