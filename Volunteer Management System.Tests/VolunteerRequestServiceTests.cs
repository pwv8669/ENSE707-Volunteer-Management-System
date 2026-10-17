using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

// This class contains unit tests for the VolunteerRequestService class in the Volunteer Management System.
namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class VolunteerRequestServiceTests
    {
        // Test that submitting a request with valid ids stores it in the service.
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

        // Test that looking up an existing request by id returns that request.
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

        // Test that looking up an unknown id returns null.
        [TestMethod]
        public void FindRequestById_WithUnknownId_ReturnsNull()
        {
            VolunteerRequestService service = new();

            VolunteerRequest? result =
                service.FindRequestById(Guid.NewGuid());

            Assert.IsNull(result);
        }

        // Test that getting requests for an opportunity only returns requests for that opportunity.
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

        // Test that getting requests for a volunteer only returns that volunteer's requests.
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

        // Test that fulfilling an existing request through the service sets its status to Fulfilled.
        [TestMethod]
        public void FulfillRequest_WithExistingId_SetsFulfilledStatus()
        {
            VolunteerRequestService service = new();
            VolunteerRequest request =
                service.SubmitRequest(Guid.NewGuid(), Guid.NewGuid());

            service.FulfillRequest(request.Id);

            VolunteerRequest? updatedRequest =
                service.FindRequestById(request.Id);

            Assert.IsNotNull(updatedRequest);
            Assert.AreEqual(
                VolunteerRequestStatus.Fulfilled,
                updatedRequest.Status);
        }

        // Test that fulfilling an unknown request throws a KeyNotFoundException.
        [TestMethod]
        public void FulfillRequest_WithUnknownId_ThrowsException()
        {
            VolunteerRequestService service = new();

            KeyNotFoundException exception =
                Assert.ThrowsExactly<KeyNotFoundException>(() =>
                    service.FulfillRequest(Guid.NewGuid()));

            StringAssert.Contains(
                exception.Message,
                "was not found");
        }

        // Test that declining an existing request through the service sets its status to Declined.
        [TestMethod]
        public void DeclineRequest_WithExistingId_SetsDeclinedStatus()
        {
            VolunteerRequestService service = new();
            VolunteerRequest request =
                service.SubmitRequest(Guid.NewGuid(), Guid.NewGuid());

            service.DeclineRequest(request.Id);

            VolunteerRequest? updatedRequest =
                service.FindRequestById(request.Id);

            Assert.IsNotNull(updatedRequest);
            Assert.AreEqual(
                VolunteerRequestStatus.Declined,
                updatedRequest.Status);
        }

        // Test that declining an unknown request throws a KeyNotFoundException.
        [TestMethod]
        public void DeclineRequest_WithUnknownId_ThrowsException()
        {
            VolunteerRequestService service = new();

            KeyNotFoundException exception =
                Assert.ThrowsExactly<KeyNotFoundException>(() =>
                    service.DeclineRequest(Guid.NewGuid()));

            StringAssert.Contains(
                exception.Message,
                "was not found");
        }

        // Test that logging hours through the service on a fulfilled request records them on the request.
        [TestMethod]
        public void LogHours_WithExistingFulfilledRequest_AccumulatesHours()
        {
            VolunteerRequestService service = new();
            VolunteerRequest request =
                service.SubmitRequest(Guid.NewGuid(), Guid.NewGuid());
            service.FulfillRequest(request.Id);

            service.LogHours(request.Id, 4);

            VolunteerRequest? updatedRequest =
                service.FindRequestById(request.Id);

            Assert.IsNotNull(updatedRequest);
            Assert.AreEqual(4.0, updatedRequest.HoursLogged);
        }

        // Test that logging hours on an unknown request throws a KeyNotFoundException.
        [TestMethod]
        public void LogHours_WithUnknownId_ThrowsException()
        {
            VolunteerRequestService service = new();

            KeyNotFoundException exception =
                Assert.ThrowsExactly<KeyNotFoundException>(() =>
                    service.LogHours(Guid.NewGuid(), 2));

            StringAssert.Contains(
                exception.Message,
                "was not found");
        }

        // Test that asking for an opportunity nobody has requested gives an empty list, not an error.
        [TestMethod]
        public void GetRequestsForOpportunity_WithNoMatches_ReturnsEmptyList()
        {
            VolunteerRequestService service = new();
            service.SubmitRequest(Guid.NewGuid(), Guid.NewGuid());

            IReadOnlyList<VolunteerRequest> requests =
                service.GetRequestsForOpportunity(Guid.NewGuid());

            Assert.HasCount(0, requests);
        }

        // Test that asking for a volunteer who has made no requests gives an empty list, not an error.
        [TestMethod]
        public void GetRequestsForVolunteer_WithNoMatches_ReturnsEmptyList()
        {
            VolunteerRequestService service = new();
            service.SubmitRequest(Guid.NewGuid(), Guid.NewGuid());

            IReadOnlyList<VolunteerRequest> requests =
                service.GetRequestsForVolunteer(Guid.NewGuid());

            Assert.HasCount(0, requests);
        }

        // Test that the service passes on the rule that a fulfilled request cannot then be declined.
        [TestMethod]
        public void DeclineRequest_AfterFulfilling_ThrowsInvalidOperationException()
        {
            VolunteerRequestService service = new();
            VolunteerRequest request =
                service.SubmitRequest(Guid.NewGuid(), Guid.NewGuid());
            service.FulfillRequest(request.Id);

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                service.DeclineRequest(request.Id));

            Assert.AreEqual(
                VolunteerRequestStatus.Fulfilled,
                service.FindRequestById(request.Id)!.Status);
        }

        // Test that negative hours are rejected by the service and nothing is logged.
        [TestMethod]
        public void LogHours_WithNegativeHours_ThrowsArgumentOutOfRangeException()
        {
            VolunteerRequestService service = new();
            VolunteerRequest request =
                service.SubmitRequest(Guid.NewGuid(), Guid.NewGuid());
            service.FulfillRequest(request.Id);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                service.LogHours(request.Id, -2));

            Assert.AreEqual(0.0, service.FindRequestById(request.Id)!.HoursLogged);
        }

        // Test that replacing all requests removes the old ones and keeps only the new ones.
        [TestMethod]
        public void ReplaceAll_WithNewRequests_RemovesTheOldOnes()
        {
            VolunteerRequestService service = new();
            VolunteerRequest old = service.SubmitRequest(Guid.NewGuid(), Guid.NewGuid());
            VolunteerRequest replacement = VolunteerRequest.Create(Guid.NewGuid(), Guid.NewGuid());

            service.ReplaceAll([replacement]);

            Assert.HasCount(1, service.GetAllRequests());
            Assert.IsNull(service.FindRequestById(old.Id));
            Assert.IsNotNull(service.FindRequestById(replacement.Id));
        }

        // Test that replacing with an empty list clears every request.
        [TestMethod]
        public void ReplaceAll_WithEmptyList_ClearsEveryRequest()
        {
            VolunteerRequestService service = new();
            service.SubmitRequest(Guid.NewGuid(), Guid.NewGuid());

            service.ReplaceAll([]);

            Assert.HasCount(0, service.GetAllRequests());
        }

        // Test that replacing with no list throws an ArgumentNullException and keeps the existing requests.
        [TestMethod]
        public void ReplaceAll_WithNull_ThrowsArgumentNullExceptionAndKeepsRequests()
        {
            VolunteerRequestService service = new();
            service.SubmitRequest(Guid.NewGuid(), Guid.NewGuid());

            Assert.ThrowsExactly<ArgumentNullException>(() => service.ReplaceAll(null!));

            Assert.HasCount(1, service.GetAllRequests());
        }

        // Test that changing the list that was passed in afterwards does not change what the service stores.
        [TestMethod]
        public void ReplaceAll_CopiesTheListItWasGiven()
        {
            VolunteerRequestService service = new();
            List<VolunteerRequest> source = [VolunteerRequest.Create(Guid.NewGuid(), Guid.NewGuid())];

            service.ReplaceAll(source);
            source.Clear();

            Assert.HasCount(1, service.GetAllRequests());
        }

        // Test that a list returned earlier is a snapshot: it does not change when requests are added or replaced afterwards.
        [TestMethod]
        public void GetAllRequests_ReturnsASnapshotThatLaterChangesDoNotAffect()
        {
            VolunteerRequestService service = new();
            service.SubmitRequest(Guid.NewGuid(), Guid.NewGuid());
            IReadOnlyList<VolunteerRequest> snapshot = service.GetAllRequests();

            service.SubmitRequest(Guid.NewGuid(), Guid.NewGuid());
            service.ReplaceAll([]);

            Assert.HasCount(1, snapshot);
        }

        // Test that many requests submitted at the same time are all kept, because the service is shared by every signed-in user.
        [TestMethod]
        public void SubmitRequest_FromManyThreadsAtOnce_KeepsEveryRequest()
        {
            VolunteerRequestService service = new();

            Parallel.For(0, 500, _ => service.SubmitRequest(Guid.NewGuid(), Guid.NewGuid()));

            Assert.HasCount(500, service.GetAllRequests());
        }

        // Test that reading while the requests are being replaced never fails or returns a half-built list.
        [TestMethod]
        public void ReplaceAll_WhileOtherThreadsRead_NeverFails()
        {
            VolunteerRequestService service = new();
            VolunteerRequest first = VolunteerRequest.Create(Guid.NewGuid(), Guid.NewGuid());
            VolunteerRequest second = VolunteerRequest.Create(Guid.NewGuid(), Guid.NewGuid());

            Parallel.For(0, 400, index =>
            {
                if (index % 2 == 0)
                {
                    service.ReplaceAll(index % 4 == 0 ? [first] : [first, second]);
                }
                else
                {
                    int count = service.GetAllRequests().Count;
                    Assert.IsLessThanOrEqualTo(2, count);
                }
            });
        }
    }
}
