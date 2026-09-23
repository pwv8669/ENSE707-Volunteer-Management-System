using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

// This class contains unit tests for the VolunteerRequest class in the Volunteer Management System.
namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class VolunteerRequestTests
    {
        // Test that creating a request with valid ids gives a new pending request with no response time and no hours.
        [TestMethod]
        public void Create_WithValidIds_CreatesPendingRequest()
        {
            Guid volunteerId = Guid.NewGuid();
            Guid opportunityId = Guid.NewGuid();

            VolunteerRequest request =
                VolunteerRequest.Create(volunteerId, opportunityId);

            Assert.AreNotEqual(Guid.Empty, request.Id);
            Assert.AreEqual(volunteerId, request.VolunteerId);
            Assert.AreEqual(opportunityId, request.OpportunityId);
            Assert.AreEqual(
                VolunteerRequestStatus.Pending,
                request.Status);
            Assert.IsNull(request.RespondedAt);
            Assert.AreEqual(0.0, request.HoursLogged);
        }

        // Test that creating a request with an empty volunteer id throws an ArgumentException.
        [TestMethod]
        public void Create_WithEmptyVolunteerId_ThrowsArgumentException()
        {
            ArgumentException exception =
                Assert.ThrowsExactly<ArgumentException>(() =>
                    VolunteerRequest.Create(Guid.Empty, Guid.NewGuid()));

            StringAssert.Contains(
                exception.Message,
                "Volunteer id is required");
        }

        // Test that creating a request with an empty opportunity id throws an ArgumentException.
        [TestMethod]
        public void Create_WithEmptyOpportunityId_ThrowsArgumentException()
        {
            ArgumentException exception =
                Assert.ThrowsExactly<ArgumentException>(() =>
                    VolunteerRequest.Create(Guid.NewGuid(), Guid.Empty));

            StringAssert.Contains(
                exception.Message,
                "Opportunity id is required");
        }

        // Test that fulfilling a pending request sets its status to Fulfilled and records the response time.
        [TestMethod]
        public void Fulfill_WithPendingRequest_SetsFulfilledStatus()
        {
            VolunteerRequest request =
                VolunteerRequest.Create(Guid.NewGuid(), Guid.NewGuid());

            request.Fulfill();

            Assert.AreEqual(
                VolunteerRequestStatus.Fulfilled,
                request.Status);
            Assert.IsNotNull(request.RespondedAt);
        }

        // Test that fulfilling a request that is already fulfilled throws an InvalidOperationException.
        [TestMethod]
        public void Fulfill_WithAlreadyFulfilledRequest_ThrowsInvalidOperationException()
        {
            VolunteerRequest request =
                VolunteerRequest.Create(Guid.NewGuid(), Guid.NewGuid());
            request.Fulfill();

            InvalidOperationException exception =
                Assert.ThrowsExactly<InvalidOperationException>(() =>
                    request.Fulfill());

            StringAssert.Contains(
                exception.Message,
                "Only pending requests can be fulfilled");
        }

        // Test that declining a pending request sets its status to Declined and records the response time.
        [TestMethod]
        public void Decline_WithPendingRequest_SetsDeclinedStatus()
        {
            VolunteerRequest request =
                VolunteerRequest.Create(Guid.NewGuid(), Guid.NewGuid());

            request.Decline();

            Assert.AreEqual(
                VolunteerRequestStatus.Declined,
                request.Status);
            Assert.IsNotNull(request.RespondedAt);
        }

        // Test that declining a request that is already declined throws an InvalidOperationException.
        [TestMethod]
        public void Decline_WithAlreadyDeclinedRequest_ThrowsInvalidOperationException()
        {
            VolunteerRequest request =
                VolunteerRequest.Create(Guid.NewGuid(), Guid.NewGuid());
            request.Decline();

            InvalidOperationException exception =
                Assert.ThrowsExactly<InvalidOperationException>(() =>
                    request.Decline());

            StringAssert.Contains(
                exception.Message,
                "Only pending requests can be declined");
        }

        // Test that logging hours more than once on a fulfilled request adds them together.
        [TestMethod]
        public void LogHours_OnFulfilledRequest_AccumulatesHours()
        {
            VolunteerRequest request =
                VolunteerRequest.Create(Guid.NewGuid(), Guid.NewGuid());
            request.Fulfill();

            request.LogHours(3.5);
            request.LogHours(2);

            Assert.AreEqual(5.5, request.HoursLogged);
        }

        // Test that logging hours on a request that is still pending throws an InvalidOperationException.
        [TestMethod]
        public void LogHours_OnPendingRequest_ThrowsInvalidOperationException()
        {
            VolunteerRequest request =
                VolunteerRequest.Create(Guid.NewGuid(), Guid.NewGuid());

            InvalidOperationException exception =
                Assert.ThrowsExactly<InvalidOperationException>(() =>
                    request.LogHours(2));

            StringAssert.Contains(
                exception.Message,
                "Hours can only be logged for fulfilled requests");
        }

        // Test that logging zero hours throws an ArgumentOutOfRangeException.
        [TestMethod]
        public void LogHours_WithZeroHours_ThrowsArgumentOutOfRangeException()
        {
            VolunteerRequest request =
                VolunteerRequest.Create(Guid.NewGuid(), Guid.NewGuid());
            request.Fulfill();

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                request.LogHours(0));
        }
    }
}
