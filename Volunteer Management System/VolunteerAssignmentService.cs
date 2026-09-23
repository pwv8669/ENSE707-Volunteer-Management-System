// Purpose:
// Provides the main operations for reviewing volunteer applications and
// assigning approved volunteers to opportunities.
//
// This service handles:
// - Viewing pending applications for an opportunity.
// - Approving applications and creating assignments.
// - Rejecting applications.
// - Checking opportunity capacity.
// - Preventing duplicate assignments.
// - Checking volunteer availability.
// - Preventing overlapping assignments.
// - Retrieving and cancelling assignments.


using System;
using System.Collections.Generic;
using System.Linq;

namespace Volunteer_Management_System
{
    // Handles application review and volunteer assignment business rules.
    public class VolunteerAssignmentService
    {
        // Provides access to volunteer opportunities and their details.
        private readonly VolunteerOpportunityService _opportunityService;

        // Provides access to submitted volunteer applications.
        private readonly VolunteerApplicationService _applicationService;

        // Optional availability service used when availability checking
        // should be enforced during assignment.
        private readonly VolunteerAvailabilityService? _availabilityService;

        // Stores volunteer assignments currently managed by this service.
        private readonly List<VolunteerAssignment> _assignments = new();

        // Creates the assignment service without availability checking.
        // This constructor supports assignment workflows where availability
        // information has not been supplied.
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

        // Creates the assignment service with availability checking enabled.
        // The supplied availability service is used before approving and
        // assigning a volunteer.
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

        // Returns all Pending applications submitted for a specific
        // volunteer opportunity.
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

        // Approves a Pending volunteer application and creates an assignment.
        //
        // Before creating the assignment, the method checks:
        // - The reviewer has permission to approve applications.
        // - The application exists and is still Pending.
        // - The opportunity exists and is Published.
        // - The opportunity has remaining capacity.
        // - The volunteer is not already assigned to the opportunity.
        // - The volunteer is available, when availability checking is enabled.
        // - The assignment does not overlap another active assignment.
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

        // Rejects a volunteer application after validating that the reviewer
        // has permission to review applications.
        public void RejectApplication(
            User reviewer,
            Guid applicationId)
        {
            ValidateReviewer(reviewer);

            VolunteerApplication application =
                GetApplicationOrThrow(applicationId);

            application.Reject(reviewer.Id);
        }

        // Returns all active assignments for a specific opportunity.
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

        // Returns all active assignments belonging to a specific volunteer.
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

        // Searches for an assignment using its unique ID.
        // Returns null when no matching assignment exists.
        public VolunteerAssignment? FindAssignmentById(
            Guid assignmentId)
        {
            return _assignments.FirstOrDefault(
                assignment =>
                    assignment.Id == assignmentId);
        }

        // Calculates how many volunteer positions remain available
        // for a specific opportunity.
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

        // Cancels an existing assignment.
        // Only a Coordinator or Admin can perform this operation.
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

        // Checks whether the opportunity still has available volunteer
        // positions before another assignment is created.
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

        // Prevents the same volunteer from being actively assigned to
        // the same opportunity more than once.
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

        // Checks whether a volunteer is available for the full duration
        // of the opportunity.
        //
        // If no availability service was supplied when this service was
        // created, this validation is skipped.
        private void ValidateAvailability(
            Guid volunteerId,
            VolunteerOpportunity opportunity)
        {
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

        // Prevents a volunteer from being assigned to two opportunities
        // whose date and time ranges overlap.
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

        // Retrieves an application when an operation requires it to exist.
        // Throws an exception if the application cannot be found.
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

        // Retrieves an opportunity when an operation requires it to exist.
        // Throws an exception if the opportunity cannot be found.
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

        // Ensures that only Coordinators or Admin users are allowed to
        // review applications or manage volunteer assignments.
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