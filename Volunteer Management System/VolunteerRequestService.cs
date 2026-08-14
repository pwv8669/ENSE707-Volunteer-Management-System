using System;
using System.Collections.Generic;
using System.Linq;

namespace Volunteer_Management_System
{
    public class VolunteerRequestService
    {
        private readonly List<VolunteerRequest> _requests = new();

        public VolunteerRequest SubmitRequest(Guid volunteerId, Guid opportunityId)
        {
            VolunteerRequest request =
                VolunteerRequest.Create(volunteerId, opportunityId);

            _requests.Add(request);

            return request;
        }

        public IReadOnlyList<VolunteerRequest> GetAllRequests()
        {
            return _requests.AsReadOnly();
        }

        public VolunteerRequest? FindRequestById(Guid requestId)
        {
            return _requests.FirstOrDefault(
                request => request.Id == requestId);
        }

        public IReadOnlyList<VolunteerRequest> GetRequestsForOpportunity(Guid opportunityId)
        {
            return _requests
                .Where(request => request.OpportunityId == opportunityId)
                .ToList();
        }

        public IReadOnlyList<VolunteerRequest> GetRequestsForVolunteer(Guid volunteerId)
        {
            return _requests
                .Where(request => request.VolunteerId == volunteerId)
                .ToList();
        }

        public void FulfillRequest(Guid requestId)
        {
            VolunteerRequest request = GetRequestOrThrow(requestId);
            request.Fulfill();
        }

        public void DeclineRequest(Guid requestId)
        {
            VolunteerRequest request = GetRequestOrThrow(requestId);
            request.Decline();
        }

        public void LogHours(Guid requestId, double hours)
        {
            VolunteerRequest request = GetRequestOrThrow(requestId);
            request.LogHours(hours);
        }

        private VolunteerRequest GetRequestOrThrow(Guid requestId)
        {
            VolunteerRequest? request = FindRequestById(requestId);

            if (request == null)
            {
                throw new KeyNotFoundException(
                    "Volunteer request was not found.");
            }

            return request;
        }
    }
}
