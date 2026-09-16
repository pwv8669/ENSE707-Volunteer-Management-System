using System;
using System.Collections.Generic;
using System.Linq;

namespace Volunteer_Management_System
{
    public class VolunteerAssignmentService
    {
        private readonly VolunteerOpportunityService _opportunityService;
        private readonly VolunteerApplicationService _applicationService;

        private readonly List<VolunteerAssignment> _assignments = new();

        public VolunteerAssignmentService(
            VolunteerOpportunityService opportunityService,
            VolunteerApplicationService applicationService)
        {
            _opportunityService =
                opportunityService
                ?? throw new ArgumentNullException(
                    nameof(opportunityService));

            _applicationService =
                applicationService
                ?? throw new ArgumentNullException(
                    nameof(applicationService));
        }

        public IReadOnlyList<VolunteerApplication>
            GetPendingApplicationsForOpportunity(
                Guid opportunityId)
        {
            if (opportunityId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Opportunity id is required.",
                    nameof(opportunityId));
            }

            VolunteerOpportunity? opportunity =
                _opportunityService.FindOpportunityById(
                    opportunityId);

            if (opportunity == null)
            {
                throw new KeyNotFoundException(
                    "Volunteer opportunity was not found.");
            }

            return _applicationService
                .GetAllApplications()
                .Where(application =>
                    application.OpportunityId == opportunityId &&
                    application.Status ==
                        VolunteerApplicationStatus.Pending)
                .ToList()
                .AsReadOnly();
        }

        public VolunteerAssignment ApproveAndAssign(
            User reviewer,
            Guid applicationId)
        {
            ValidateReviewer(reviewer);

            VolunteerApplication application =
                GetApplicationOrThrow(applicationId);

            if (application.Status !=
                VolunteerApplicationStatus.Pending)
            {
                throw new InvalidOperationException(
                    "Only pending applications can be approved.");
            }

            VolunteerOpportunity opportunity =
                GetOpportunityOrThrow(
                    application.OpportunityId);

            if (opportunity.Status !=
                OpportunityStatus.Published)
            {
                throw new InvalidOperationException(
                    "Volunteers can only be assigned " +
                    "to published opportunities.");
            }

            int activeAssignments =
                _assignments.Count(assignment =>
                    assignment.OpportunityId ==
                        opportunity.Id &&
                    assignment.Status ==
                        AssignmentStatus.Assigned);

            if (activeAssignments >=
                opportunity.VolunteersNeeded)
            {
                throw new InvalidOperationException(
                    "This opportunity has reached " +
                    "its volunteer capacity.");
            }

            bool alreadyAssigned =
                _assignments.Any(assignment =>
                    assignment.VolunteerId ==
                        application.VolunteerId &&
                    assignment.OpportunityId ==
                        application.OpportunityId &&
                    assignment.Status ==
                        AssignmentStatus.Assigned);

            if (alreadyAssigned)
            {
                throw new InvalidOperationException(
                    "Volunteer is already assigned " +
                    "to this opportunity.");
            }

            VolunteerAssignment assignment =
                VolunteerAssignment.Create(
                    application.VolunteerId,
                    application.OpportunityId,
                    application.Id,
                    reviewer.Id);

            _assignments.Add(assignment);

            application.Approve(reviewer.Id);

            return assignment;
        }

        public void RejectApplication(
            User reviewer,
            Guid applicationId)
        {
            ValidateReviewer(reviewer);

            VolunteerApplication application =
                GetApplicationOrThrow(applicationId);

            application.Reject(reviewer.Id);
        }

        public IReadOnlyList<VolunteerAssignment>
            GetAssignmentsForOpportunity(
                Guid opportunityId)
        {
            return _assignments
                .Where(assignment =>
                    assignment.OpportunityId ==
                        opportunityId &&
                    assignment.Status ==
                        AssignmentStatus.Assigned)
                .ToList()
                .AsReadOnly();
        }

        public IReadOnlyList<VolunteerAssignment>
            GetAssignmentsForVolunteer(
                Guid volunteerId)
        {
            return _assignments
                .Where(assignment =>
                    assignment.VolunteerId ==
                        volunteerId &&
                    assignment.Status ==
                        AssignmentStatus.Assigned)
                .ToList()
                .AsReadOnly();
        }

        public VolunteerAssignment? FindAssignmentById(
            Guid assignmentId)
        {
            return _assignments.FirstOrDefault(
                assignment =>
                    assignment.Id == assignmentId);
        }

        public void CancelAssignment(
            User reviewer,
            Guid assignmentId)
        {
            ValidateReviewer(reviewer);

            VolunteerAssignment? assignment =
                FindAssignmentById(assignmentId);

            if (assignment == null)
            {
                throw new KeyNotFoundException(
                    "Volunteer assignment was not found.");
            }

            assignment.Cancel();
        }

        private VolunteerApplication GetApplicationOrThrow(
            Guid applicationId)
        {
            VolunteerApplication? application =
                _applicationService.FindApplicationById(
                    applicationId);

            if (application == null)
            {
                throw new KeyNotFoundException(
                    "Volunteer application was not found.");
            }

            return application;
        }

        private VolunteerOpportunity GetOpportunityOrThrow(
            Guid opportunityId)
        {
            VolunteerOpportunity? opportunity =
                _opportunityService.FindOpportunityById(
                    opportunityId);

            if (opportunity == null)
            {
                throw new KeyNotFoundException(
                    "Volunteer opportunity was not found.");
            }

            return opportunity;
        }

        private static void ValidateReviewer(
            User reviewer)
        {
            if (reviewer == null)
            {
                throw new ArgumentNullException(
                    nameof(reviewer));
            }

            if (reviewer.Role != Role.Coordinator &&
                reviewer.Role != Role.Admin)
            {
                throw new UnauthorizedAccessException(
                    "Only coordinators or administrators " +
                    "can review applications.");
            }
        }
    }
}