using System;
using System.Collections.Generic;
using System.Linq;

namespace Volunteer_Management_System
{
    public class VolunteerAssignmentService
    {
        private readonly VolunteerOpportunityService _opportunityService;
        private readonly VolunteerApplicationService _applicationService;
        private readonly VolunteerAvailabilityService? _availabilityService;

        private readonly List<VolunteerAssignment> _assignments = new();

        // Constructor used by the existing Feature 4 tests.
        // Availability checking is optional here.
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

            _availabilityService = null;
        }

        // Constructor used when availability checking is required.
        public VolunteerAssignmentService(
            VolunteerOpportunityService opportunityService,
            VolunteerApplicationService applicationService,
            VolunteerAvailabilityService availabilityService)
        {
            _opportunityService =
                opportunityService
                ?? throw new ArgumentNullException(
                    nameof(opportunityService));

            _applicationService =
                applicationService
                ?? throw new ArgumentNullException(
                    nameof(applicationService));

            _availabilityService =
                availabilityService
                ?? throw new ArgumentNullException(
                    nameof(availabilityService));
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

            ValidateCapacity(opportunity);

            ValidateNotAlreadyAssigned(application);

            ValidateAvailability(
                application.VolunteerId,
                opportunity);

            ValidateNoAssignmentConflict(
                application.VolunteerId,
                opportunity);

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

        public int GetRemainingCapacity(
            Guid opportunityId)
        {
            VolunteerOpportunity opportunity =
                GetOpportunityOrThrow(opportunityId);

            int assignedCount =
                _assignments.Count(assignment =>
                    assignment.OpportunityId ==
                        opportunityId &&
                    assignment.Status ==
                        AssignmentStatus.Assigned);

            return Math.Max(
                0,
                opportunity.VolunteersNeeded -
                assignedCount);
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

        private void ValidateCapacity(
            VolunteerOpportunity opportunity)
        {
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
        }

        private void ValidateNotAlreadyAssigned(
            VolunteerApplication application)
        {
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
        }

        private void ValidateAvailability(
            Guid volunteerId,
            VolunteerOpportunity opportunity)
        {
            // If the service was created without an availability
            // service, retain the behaviour from the previous commit.
            if (_availabilityService == null)
            {
                return;
            }

            bool available =
                _availabilityService.IsVolunteerAvailable(
                    volunteerId,
                    opportunity.StartDateTime,
                    opportunity.EndDateTime);

            if (!available)
            {
                throw new InvalidOperationException(
                    "Volunteer is not available " +
                    "during this opportunity.");
            }
        }

        private void ValidateNoAssignmentConflict(
            Guid volunteerId,
            VolunteerOpportunity newOpportunity)
        {
            IReadOnlyList<VolunteerAssignment>
                volunteerAssignments =
                    GetAssignmentsForVolunteer(
                        volunteerId);

            foreach (VolunteerAssignment assignment
                in volunteerAssignments)
            {
                VolunteerOpportunity? existingOpportunity =
                    _opportunityService.FindOpportunityById(
                        assignment.OpportunityId);

                if (existingOpportunity == null)
                {
                    continue;
                }

                bool overlaps =
                    newOpportunity.StartDateTime <
                        existingOpportunity.EndDateTime &&
                    newOpportunity.EndDateTime >
                        existingOpportunity.StartDateTime;

                if (overlaps)
                {
                    throw new InvalidOperationException(
                        "Volunteer already has an " +
                        "overlapping assignment.");
                }
            }
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