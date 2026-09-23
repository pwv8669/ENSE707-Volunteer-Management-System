using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

// This class contains unit tests for the ReportingService class in the Volunteer Management System.
namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class ReportingServiceTests
    {
        // Helper that creates a sample opportunity (a beach cleanup a week from now that needs 10 volunteers) for the tests to use.
        private static VolunteerOpportunity CreateOpportunity(
            VolunteerOpportunityService opportunityService)
        {
            DateTime startTime = DateTime.UtcNow.AddDays(7);

            return opportunityService.CreateOpportunity(
                "Beach Cleanup",
                "Help clean the beach.",
                "Mission Bay",
                startTime,
                startTime.AddHours(3),
                "Teamwork",
                10);
        }

        // Test that creating the service without a request service throws an ArgumentNullException.
        [TestMethod]
        public void Constructor_WithNullRequestService_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new ReportingService(null!, new VolunteerOpportunityService()));
        }

        // Test that creating the service without an opportunity service throws an ArgumentNullException.
        [TestMethod]
        public void Constructor_WithNullOpportunityService_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new ReportingService(new VolunteerRequestService(), null!));
        }

        // Test that the participation report counts a volunteer's fulfilled and pending requests and adds up their hours.
        [TestMethod]
        public void GetVolunteerParticipationReport_WithMixedRequestStatuses_SummarizesPerVolunteer()
        {
            VolunteerRequestService requestService = new();
            VolunteerOpportunityService opportunityService = new();
            ReportingService reportingService =
                new(requestService, opportunityService);

            VolunteerOpportunity opportunity =
                CreateOpportunity(opportunityService);
            Guid volunteerId = Guid.NewGuid();

            VolunteerRequest fulfilledRequest =
                requestService.SubmitRequest(volunteerId, opportunity.Id);
            requestService.FulfillRequest(fulfilledRequest.Id);
            requestService.LogHours(fulfilledRequest.Id, 3);

            requestService.SubmitRequest(volunteerId, opportunity.Id);

            IReadOnlyList<VolunteerParticipationSummary> report =
                reportingService.GetVolunteerParticipationReport();

            VolunteerParticipationSummary summary =
                report.Single(entry => entry.VolunteerId == volunteerId);

            Assert.AreEqual(2, summary.TotalRequests);
            Assert.AreEqual(1, summary.FulfilledRequests);
            Assert.AreEqual(1, summary.PendingRequests);
            Assert.AreEqual(0, summary.DeclinedRequests);
            Assert.AreEqual(3.0, summary.TotalHoursLogged);
        }

        // Test that the participation report is empty when there are no requests.
        [TestMethod]
        public void GetVolunteerParticipationReport_WithNoRequests_ReturnsEmptyList()
        {
            ReportingService reportingService = new(
                new VolunteerRequestService(),
                new VolunteerOpportunityService());

            IReadOnlyList<VolunteerParticipationSummary> report =
                reportingService.GetVolunteerParticipationReport();

            Assert.HasCount(0, report);
        }

        // Test that event statistics count an opportunity's fulfilled, declined and pending requests and add up its hours.
        [TestMethod]
        public void GetEventStatistics_WithExistingOpportunity_ReturnsAggregatedCounts()
        {
            VolunteerRequestService requestService = new();
            VolunteerOpportunityService opportunityService = new();
            ReportingService reportingService =
                new(requestService, opportunityService);

            VolunteerOpportunity opportunity =
                CreateOpportunity(opportunityService);

            VolunteerRequest fulfilledRequest =
                requestService.SubmitRequest(Guid.NewGuid(), opportunity.Id);
            requestService.FulfillRequest(fulfilledRequest.Id);
            requestService.LogHours(fulfilledRequest.Id, 2.5);

            VolunteerRequest declinedRequest =
                requestService.SubmitRequest(Guid.NewGuid(), opportunity.Id);
            requestService.DeclineRequest(declinedRequest.Id);

            requestService.SubmitRequest(Guid.NewGuid(), opportunity.Id);

            EventStatistics statistics =
                reportingService.GetEventStatistics(opportunity.Id);

            Assert.AreEqual(opportunity.Id, statistics.OpportunityId);
            Assert.AreEqual(opportunity.Title, statistics.Title);
            Assert.AreEqual(10, statistics.VolunteersNeeded);
            Assert.AreEqual(3, statistics.RequestsReceived);
            Assert.AreEqual(1, statistics.FulfilledRequests);
            Assert.AreEqual(1, statistics.PendingRequests);
            Assert.AreEqual(1, statistics.DeclinedRequests);
            Assert.AreEqual(2.5, statistics.TotalHoursLogged);
        }

        // Test that asking for statistics on an unknown opportunity throws a KeyNotFoundException.
        [TestMethod]
        public void GetEventStatistics_WithUnknownOpportunity_ThrowsException()
        {
            ReportingService reportingService = new(
                new VolunteerRequestService(),
                new VolunteerOpportunityService());

            KeyNotFoundException exception =
                Assert.ThrowsExactly<KeyNotFoundException>(() =>
                    reportingService.GetEventStatistics(Guid.NewGuid()));

            StringAssert.Contains(
                exception.Message,
                "was not found");
        }

        // Test that only pending requests are returned when requests have mixed statuses.
        [TestMethod]
        public void GetPendingRequests_WithMixedRequestStatuses_ReturnsOnlyPendingRequests()
        {
            VolunteerRequestService requestService = new();
            VolunteerOpportunityService opportunityService = new();
            ReportingService reportingService =
                new(requestService, opportunityService);

            VolunteerOpportunity opportunity =
                CreateOpportunity(opportunityService);

            VolunteerRequest pendingRequest =
                requestService.SubmitRequest(Guid.NewGuid(), opportunity.Id);

            VolunteerRequest fulfilledRequest =
                requestService.SubmitRequest(Guid.NewGuid(), opportunity.Id);
            requestService.FulfillRequest(fulfilledRequest.Id);

            VolunteerRequest declinedRequest =
                requestService.SubmitRequest(Guid.NewGuid(), opportunity.Id);
            requestService.DeclineRequest(declinedRequest.Id);

            IReadOnlyList<VolunteerRequest> pendingRequests =
                reportingService.GetPendingRequests();

            Assert.HasCount(1, pendingRequests);
            Assert.AreEqual(pendingRequest.Id, pendingRequests[0].Id);
        }

        // Test that only fulfilled requests are returned when requests have mixed statuses.
        [TestMethod]
        public void GetFulfilledRequests_WithMixedRequestStatuses_ReturnsOnlyFulfilledRequests()
        {
            VolunteerRequestService requestService = new();
            VolunteerOpportunityService opportunityService = new();
            ReportingService reportingService =
                new(requestService, opportunityService);

            VolunteerOpportunity opportunity =
                CreateOpportunity(opportunityService);

            requestService.SubmitRequest(Guid.NewGuid(), opportunity.Id);

            VolunteerRequest fulfilledRequest =
                requestService.SubmitRequest(Guid.NewGuid(), opportunity.Id);
            requestService.FulfillRequest(fulfilledRequest.Id);

            VolunteerRequest declinedRequest =
                requestService.SubmitRequest(Guid.NewGuid(), opportunity.Id);
            requestService.DeclineRequest(declinedRequest.Id);

            IReadOnlyList<VolunteerRequest> fulfilledRequests =
                reportingService.GetFulfilledRequests();

            Assert.HasCount(1, fulfilledRequests);
            Assert.AreEqual(fulfilledRequest.Id, fulfilledRequests[0].Id);
        }

        // Test that no pending requests are returned when there are no requests.
        [TestMethod]
        public void GetPendingRequests_WithNoRequests_ReturnsEmptyList()
        {
            ReportingService reportingService = new(
                new VolunteerRequestService(),
                new VolunteerOpportunityService());

            IReadOnlyList<VolunteerRequest> pendingRequests =
                reportingService.GetPendingRequests();

            Assert.HasCount(0, pendingRequests);
        }

        // Test that no fulfilled requests are returned when there are no requests.
        [TestMethod]
        public void GetFulfilledRequests_WithNoRequests_ReturnsEmptyList()
        {
            ReportingService reportingService = new(
                new VolunteerRequestService(),
                new VolunteerOpportunityService());

            IReadOnlyList<VolunteerRequest> fulfilledRequests =
                reportingService.GetFulfilledRequests();

            Assert.HasCount(0, fulfilledRequests);
        }
    }
}
