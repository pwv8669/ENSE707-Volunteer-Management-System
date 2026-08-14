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
    }
}
