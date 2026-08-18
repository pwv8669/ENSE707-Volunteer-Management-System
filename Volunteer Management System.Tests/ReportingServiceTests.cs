using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class ReportingServiceTests
    {
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

        [TestMethod]
        public void Constructor_WithNullRequestService_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new ReportingService(null!, new VolunteerOpportunityService()));
        }

        [TestMethod]
        public void Constructor_WithNullOpportunityService_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new ReportingService(new VolunteerRequestService(), null!));
        }

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
    }
}
