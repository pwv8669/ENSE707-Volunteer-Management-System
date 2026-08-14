using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class VolunteerRequestServiceTests
    {
        [TestMethod]
        public void SubmitRequest_WithValidIds_StoresRequest()
        {
            VolunteerRequestService service = new();
            Guid volunteerId = Guid.NewGuid();
            Guid opportunityId = Guid.NewGuid();

            VolunteerRequest submittedRequest =
                service.SubmitRequest(volunteerId, opportunityId);

            IReadOnlyList<VolunteerRequest> requests =
                service.GetAllRequests();

            Assert.HasCount(1, requests);
            Assert.AreEqual(submittedRequest.Id, requests[0].Id);
        }

        [TestMethod]
        public void FindRequestById_WithExistingId_ReturnsRequest()
        {
            VolunteerRequestService service = new();

            VolunteerRequest submittedRequest =
                service.SubmitRequest(Guid.NewGuid(), Guid.NewGuid());

            VolunteerRequest? foundRequest =
                service.FindRequestById(submittedRequest.Id);

            Assert.IsNotNull(foundRequest);
            Assert.AreEqual(submittedRequest.Id, foundRequest.Id);
        }

        [TestMethod]
        public void FindRequestById_WithUnknownId_ReturnsNull()
        {
            VolunteerRequestService service = new();

            VolunteerRequest? result =
                service.FindRequestById(Guid.NewGuid());

            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetRequestsForOpportunity_WithMultipleOpportunities_ReturnsMatchingOnly()
        {
            VolunteerRequestService service = new();
            Guid opportunityId = Guid.NewGuid();

            service.SubmitRequest(Guid.NewGuid(), opportunityId);
            service.SubmitRequest(Guid.NewGuid(), opportunityId);
            service.SubmitRequest(Guid.NewGuid(), Guid.NewGuid());

            IReadOnlyList<VolunteerRequest> requests =
                service.GetRequestsForOpportunity(opportunityId);

            Assert.HasCount(2, requests);
        }

        [TestMethod]
        public void GetRequestsForVolunteer_WithMultipleVolunteers_ReturnsMatchingOnly()
        {
            VolunteerRequestService service = new();
            Guid volunteerId = Guid.NewGuid();

            service.SubmitRequest(volunteerId, Guid.NewGuid());
            service.SubmitRequest(volunteerId, Guid.NewGuid());
            service.SubmitRequest(Guid.NewGuid(), Guid.NewGuid());

            IReadOnlyList<VolunteerRequest> requests =
                service.GetRequestsForVolunteer(volunteerId);

            Assert.HasCount(2, requests);
        }
    }
}
