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

        // Test that logging negative hours throws an ArgumentOutOfRangeException and leaves the total unchanged.
        [TestMethod]
        public void LogHours_WithNegativeHours_ThrowsArgumentOutOfRangeException()
        {
            VolunteerRequest request =
                VolunteerRequest.Create(Guid.NewGuid(), Guid.NewGuid());
            request.Fulfill();
            request.LogHours(2);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                request.LogHours(-1));

            Assert.AreEqual(2.0, request.HoursLogged);
        }

        // Test the lower boundary: the smallest sensible positive amount of hours (0.01) is accepted.
        [TestMethod]
        public void LogHours_WithVerySmallPositiveHours_IsAccepted()
        {
            VolunteerRequest request =
                VolunteerRequest.Create(Guid.NewGuid(), Guid.NewGuid());
            request.Fulfill();

            request.LogHours(0.01);

            Assert.AreEqual(0.01, request.HoursLogged);
        }

        // Test that a declined request cannot be fulfilled afterwards.
        [TestMethod]
        public void Fulfill_OnDeclinedRequest_ThrowsInvalidOperationException()
        {
            VolunteerRequest request =
                VolunteerRequest.Create(Guid.NewGuid(), Guid.NewGuid());
            request.Decline();

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                request.Fulfill());

            Assert.AreEqual(VolunteerRequestStatus.Declined, request.Status);
        }

        // Test that a fulfilled request cannot be declined afterwards.
        [TestMethod]
        public void Decline_OnFulfilledRequest_ThrowsInvalidOperationException()
        {
            VolunteerRequest request =
                VolunteerRequest.Create(Guid.NewGuid(), Guid.NewGuid());
            request.Fulfill();

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                request.Decline());

            Assert.AreEqual(VolunteerRequestStatus.Fulfilled, request.Status);
        }

        // Test that hours cannot be logged against a declined request.
        [TestMethod]
        public void LogHours_OnDeclinedRequest_ThrowsInvalidOperationException()
        {
            VolunteerRequest request =
                VolunteerRequest.Create(Guid.NewGuid(), Guid.NewGuid());
            request.Decline();

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                request.LogHours(2));

            Assert.AreEqual(0.0, request.HoursLogged);
        }

        // Test that responding to a request records a UTC response time that is not before the request time.
        [TestMethod]
        public void Fulfill_RecordsUtcResponseTime()
        {
            VolunteerRequest request =
                VolunteerRequest.Create(Guid.NewGuid(), Guid.NewGuid());

            request.Fulfill();

            Assert.IsNotNull(request.RespondedAt);
            Assert.IsTrue(request.RespondedAt >= request.RequestedAt);
            Assert.IsTrue(request.RespondedAt <= DateTime.UtcNow);
        }

        // Test that every new request gets its own unique id, even for the same volunteer and opportunity.
        [TestMethod]
        public void Create_TwoRequestsForTheSamePair_HaveDifferentIds()
        {
            Guid volunteerId = Guid.NewGuid();
            Guid opportunityId = Guid.NewGuid();

            VolunteerRequest first = VolunteerRequest.Create(volunteerId, opportunityId);
            VolunteerRequest second = VolunteerRequest.Create(volunteerId, opportunityId);

            Assert.AreNotEqual(first.Id, second.Id);
        }

        // Test that restoring a pending request keeps the given id and times and leaves it pending with no hours.
        [TestMethod]
        public void Restore_WithPendingState_RebuildsPendingRequest()
        {
            Guid id = Guid.NewGuid();
            Guid volunteerId = Guid.NewGuid();
            Guid opportunityId = Guid.NewGuid();
            DateTime requestedAt = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);

            VolunteerRequest request = VolunteerRequest.Restore(
                id, volunteerId, opportunityId, VolunteerRequestStatus.Pending, requestedAt, null, 0);

            Assert.AreEqual(id, request.Id);
            Assert.AreEqual(volunteerId, request.VolunteerId);
            Assert.AreEqual(opportunityId, request.OpportunityId);
            Assert.AreEqual(VolunteerRequestStatus.Pending, request.Status);
            Assert.AreEqual(requestedAt, request.RequestedAt);
            Assert.IsNull(request.RespondedAt);
            Assert.AreEqual(0.0, request.HoursLogged);
        }

        // Test that restoring a fulfilled request keeps its response time and hours.
        [TestMethod]
        public void Restore_WithFulfilledState_KeepsRespondedAtAndHours()
        {
            DateTime requestedAt = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
            DateTime respondedAt = requestedAt.AddDays(1);

            VolunteerRequest request = VolunteerRequest.Restore(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                VolunteerRequestStatus.Fulfilled, requestedAt, respondedAt, 3.5);

            Assert.AreEqual(VolunteerRequestStatus.Fulfilled, request.Status);
            Assert.AreEqual(respondedAt, request.RespondedAt);
            Assert.AreEqual(3.5, request.HoursLogged);
        }

        // Test that a fulfilled request that has no hours yet can be restored (hours are optional until attendance is recorded).
        [TestMethod]
        public void Restore_WithFulfilledStateAndZeroHours_IsAccepted()
        {
            VolunteerRequest request = VolunteerRequest.Restore(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                VolunteerRequestStatus.Fulfilled, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 0);

            Assert.AreEqual(0.0, request.HoursLogged);
        }

        // Test that restoring a declined request keeps its response time and has no hours.
        [TestMethod]
        public void Restore_WithDeclinedState_RebuildsDeclinedRequest()
        {
            VolunteerRequest request = VolunteerRequest.Restore(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                VolunteerRequestStatus.Declined, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 0);

            Assert.AreEqual(VolunteerRequestStatus.Declined, request.Status);
            Assert.IsNotNull(request.RespondedAt);
        }

        // Test that a restored request can't be changed in ways the normal rules forbid, for example declined after it was fulfilled.
        [TestMethod]
        public void Restore_WithFulfilledState_StillFollowsTheNormalRules()
        {
            VolunteerRequest request = VolunteerRequest.Restore(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                VolunteerRequestStatus.Fulfilled, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 1);

            Assert.ThrowsExactly<InvalidOperationException>(() => request.Decline());
        }

        // Test that restoring with an empty request id throws an ArgumentException.
        [TestMethod]
        public void Restore_WithEmptyId_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                VolunteerRequest.Restore(
                    Guid.Empty, Guid.NewGuid(), Guid.NewGuid(),
                    VolunteerRequestStatus.Pending, DateTime.UtcNow, null, 0));
        }

        // Test that restoring with an empty volunteer id throws an ArgumentException.
        [TestMethod]
        public void Restore_WithEmptyVolunteerId_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                VolunteerRequest.Restore(
                    Guid.NewGuid(), Guid.Empty, Guid.NewGuid(),
                    VolunteerRequestStatus.Pending, DateTime.UtcNow, null, 0));
        }

        // Test that restoring with an empty opportunity id throws an ArgumentException.
        [TestMethod]
        public void Restore_WithEmptyOpportunityId_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                VolunteerRequest.Restore(
                    Guid.NewGuid(), Guid.NewGuid(), Guid.Empty,
                    VolunteerRequestStatus.Pending, DateTime.UtcNow, null, 0));
        }

        // Test that negative hours are rejected when restoring.
        [TestMethod]
        public void Restore_WithNegativeHours_ThrowsArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                VolunteerRequest.Restore(
                    Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                    VolunteerRequestStatus.Fulfilled, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, -1));
        }

        // Test that a pending request can't be restored with a response time, because it hasn't been answered.
        [TestMethod]
        public void Restore_PendingWithResponseTime_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                VolunteerRequest.Restore(
                    Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                    VolunteerRequestStatus.Pending, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 0));
        }

        // Test that a pending request can't be restored with hours.
        [TestMethod]
        public void Restore_PendingWithHours_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                VolunteerRequest.Restore(
                    Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                    VolunteerRequestStatus.Pending, DateTime.UtcNow, null, 2));
        }

        // Test that a fulfilled request can't be restored without a response time.
        [TestMethod]
        public void Restore_FulfilledWithoutResponseTime_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                VolunteerRequest.Restore(
                    Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                    VolunteerRequestStatus.Fulfilled, DateTime.UtcNow, null, 0));
        }

        // Test that a declined request can't be restored with hours.
        [TestMethod]
        public void Restore_DeclinedWithHours_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                VolunteerRequest.Restore(
                    Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                    VolunteerRequestStatus.Declined, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 2));
        }
    }
}
