// Purpose:
// Builds the schedule shown to a volunteer from their active assignments and
// the date, time and location stored on each volunteer opportunity.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Volunteer_Management_System
{
    // A single scheduled shift displayed on the volunteer's schedule.
    public sealed record VolunteerScheduledShift(
        Guid AssignmentId,
        Guid OpportunityId,
        string Title,
        string Location,
        DateTime StartsAt,
        DateTime EndsAt,
        string RequiredSkills);

    // Joins active assignments with their opportunity details and returns
    // them in chronological order.
    public class VolunteerScheduleService
    {
        private readonly VolunteerAssignmentService _assignmentService;
        private readonly VolunteerOpportunityService _opportunityService;

        public VolunteerScheduleService(
            VolunteerAssignmentService assignmentService,
            VolunteerOpportunityService opportunityService)
        {
            _assignmentService = assignmentService
                ?? throw new ArgumentNullException(nameof(assignmentService));
            _opportunityService = opportunityService
                ?? throw new ArgumentNullException(nameof(opportunityService));
        }

        // Returns active shifts for one volunteer. Assignments whose
        // opportunity has been deleted cannot be displayed and are omitted.
        public IReadOnlyList<VolunteerScheduledShift> GetSchedule(
            Guid volunteerId)
        {
            if (volunteerId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Volunteer id is required.",
                    nameof(volunteerId));
            }

            return _assignmentService
                .GetAssignmentsForVolunteer(volunteerId)
                .Select(assignment => new
                {
                    Assignment = assignment,
                    Opportunity = _opportunityService.FindOpportunityById(
                        assignment.OpportunityId)
                })
                .Where(item => item.Opportunity is not null)
                .Select(item => new VolunteerScheduledShift(
                    item.Assignment.Id,
                    item.Assignment.OpportunityId,
                    item.Opportunity!.Title,
                    item.Opportunity.Location,
                    item.Opportunity.StartDateTime,
                    item.Opportunity.EndDateTime,
                    item.Opportunity.RequiredSkills))
                .OrderBy(shift => shift.StartsAt)
                .ThenBy(shift => shift.Title)
                .ToList()
                .AsReadOnly();
        }
    }
}
