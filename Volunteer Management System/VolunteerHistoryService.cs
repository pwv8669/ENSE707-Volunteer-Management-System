using System;
using System.Collections.Generic;
using System.Linq;

// The purpose of this file is to build a volunteer's volunteering history:
// every request they have made, joined with the details of the event it was
// for, plus a summary of their activity and hours.
namespace Volunteer_Management_System
{
    // One row of a volunteer's history: a request they made, joined with the event it was for.
    public class VolunteerHistoryEntry
    {
        // The request this row comes from.
        public Guid RequestId { get; init; }

        // The opportunity (event) the request was for.
        public Guid OpportunityId { get; init; }

        // The event's title, or a placeholder if the event has since been deleted.
        public string EventTitle { get; init; } = string.Empty;

        // Where the event takes place (empty if the event has been deleted).
        public string Location { get; init; } = string.Empty;

        // When the event starts and ends; null if the event has been deleted.
        public DateTime? EventStart { get; init; }

        public DateTime? EventEnd { get; init; }

        // The request's current state (Pending, Fulfilled or Declined).
        public VolunteerRequestStatus Status { get; init; }

        // When the volunteer made the request, and when it was answered (null while pending).
        public DateTime RequestedAt { get; init; }

        public DateTime? RespondedAt { get; init; }

        // Hours logged for this activity.
        public double HoursLogged { get; init; }
    }

    // Totals across a volunteer's whole history.
    public class VolunteerHistorySummary
    {
        // Every request the volunteer has made.
        public int TotalActivities { get; init; }

        // How many were fulfilled, are still pending, or were declined.
        public int FulfilledActivities { get; init; }

        public int PendingActivities { get; init; }

        public int DeclinedActivities { get; init; }

        // Total hours logged across all activities.
        public double TotalHours { get; init; }
    }

    // Builds history and summaries for one volunteer from the request and opportunity services.
    // It only reads data and never changes it.
    public class VolunteerHistoryService
    {
        // Placeholder title used when a request points at an event that no longer exists.
        public const string MissingEventTitle = "Event no longer available";

        // The services the history is read from.
        private readonly VolunteerRequestService _requestService;
        private readonly VolunteerOpportunityService _opportunityService;

        // Both services are required; passing null throws ArgumentNullException.
        public VolunteerHistoryService(
            VolunteerRequestService requestService,
            VolunteerOpportunityService opportunityService)
        {
            _requestService = requestService
                ?? throw new ArgumentNullException(nameof(requestService));
            _opportunityService = opportunityService
                ?? throw new ArgumentNullException(nameof(opportunityService));
        }

        // Returns the volunteer's history, newest event first.
        // Throws ArgumentException if the volunteer id is empty.
        public IReadOnlyList<VolunteerHistoryEntry> GetHistory(Guid volunteerId)
        {
            if (volunteerId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Volunteer id is required.",
                    nameof(volunteerId));
            }

            // Sort by the event's start time; requests for deleted events fall back to when they were made.
            return _requestService
                .GetRequestsForVolunteer(volunteerId)
                .Select(CreateEntry)
                .OrderByDescending(entry => entry.EventStart ?? entry.RequestedAt)
                .ThenByDescending(entry => entry.RequestedAt)
                .ToList()
                .AsReadOnly();
        }

        // Returns activity counts by status and total hours for the volunteer.
        // Throws ArgumentException if the volunteer id is empty.
        public VolunteerHistorySummary GetSummary(Guid volunteerId)
        {
            IReadOnlyList<VolunteerHistoryEntry> history = GetHistory(volunteerId);

            return new VolunteerHistorySummary
            {
                TotalActivities = history.Count,
                FulfilledActivities = history.Count(
                    entry => entry.Status == VolunteerRequestStatus.Fulfilled),
                PendingActivities = history.Count(
                    entry => entry.Status == VolunteerRequestStatus.Pending),
                DeclinedActivities = history.Count(
                    entry => entry.Status == VolunteerRequestStatus.Declined),
                TotalHours = history.Sum(entry => entry.HoursLogged)
            };
        }

        // Joins one request with its event's details.
        private VolunteerHistoryEntry CreateEntry(VolunteerRequest request)
        {
            VolunteerOpportunity? opportunity =
                _opportunityService.FindOpportunityById(request.OpportunityId);

            return new VolunteerHistoryEntry
            {
                RequestId = request.Id,
                OpportunityId = request.OpportunityId,
                EventTitle = opportunity?.Title ?? MissingEventTitle,
                Location = opportunity?.Location ?? string.Empty,
                EventStart = opportunity?.StartDateTime,
                EventEnd = opportunity?.EndDateTime,
                Status = request.Status,
                RequestedAt = request.RequestedAt,
                RespondedAt = request.RespondedAt,
                HoursLogged = request.HoursLogged
            };
        }
    }
}
