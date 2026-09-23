using System;
using System.Collections.Generic;
using System.Linq;

// The purpose of this class is to manage volunteer requests: submitting them, looking them up, and moving them through fulfilling, declining and logging hours.
namespace Volunteer_Management_System
{
    // Stores and manages volunteer requests in memory (they do not persist between app restarts).
    public class VolunteerRequestService
    {
        // Every request submitted so far.
        private readonly List<VolunteerRequest> _requests = new();

        // Creates a new pending request for the volunteer and opportunity, stores it, and returns it.
        public VolunteerRequest SubmitRequest(Guid volunteerId, Guid opportunityId)
        {
            VolunteerRequest request =
                VolunteerRequest.Create(volunteerId, opportunityId);

            _requests.Add(request);

            return request;
        }

        // Returns every request as a read-only list.
        public IReadOnlyList<VolunteerRequest> GetAllRequests()
        {
            return _requests.AsReadOnly();
        }

        // Returns the request with the given id, or null if there isn't one.
        public VolunteerRequest? FindRequestById(Guid requestId)
        {
            return _requests.FirstOrDefault(
                request => request.Id == requestId);
        }

        // Returns all requests made for one opportunity.
        public IReadOnlyList<VolunteerRequest> GetRequestsForOpportunity(Guid opportunityId)
        {
            return _requests
                .Where(request => request.OpportunityId == opportunityId)
                .ToList();
        }

        // Returns all requests made by one volunteer.
        public IReadOnlyList<VolunteerRequest> GetRequestsForVolunteer(Guid volunteerId)
        {
            return _requests
                .Where(request => request.VolunteerId == volunteerId)
                .ToList();
        }

        // Fulfills the request with the given id. Throws KeyNotFoundException if it doesn't exist.
        public void FulfillRequest(Guid requestId)
        {
            VolunteerRequest request = GetRequestOrThrow(requestId);
            request.Fulfill();
        }

        // Declines the request with the given id. Throws KeyNotFoundException if it doesn't exist.
        public void DeclineRequest(Guid requestId)
        {
            VolunteerRequest request = GetRequestOrThrow(requestId);
            request.Decline();
        }

        // Logs hours against a fulfilled request. Throws KeyNotFoundException if it doesn't exist.
        public void LogHours(Guid requestId, double hours)
        {
            VolunteerRequest request = GetRequestOrThrow(requestId);
            request.LogHours(hours);
        }

        // Finds a request by id, or throws KeyNotFoundException if it isn't found.
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
