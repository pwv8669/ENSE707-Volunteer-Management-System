
// Purpose:
// Provides the main operations for volunteer applications.
//
// This service allows volunteers to browse published opportunities, submit
// applications, retrieve their applications and view application statuses.
//
// It also applies business rules such as:
// - Only users with the Volunteer role can apply.
// - Applications can only be submitted for Published opportunities.
// - A volunteer cannot apply to the same opportunity more than once.
// - Volunteers can only view the status of their own applications.


using System;
using System.Collections.Generic;
using System.Linq;

namespace Volunteer_Management_System
{
    // Handles volunteer application submission, retrieval and status viewing.
    public class VolunteerApplicationService
    {
        // Provides access to opportunity information needed when volunteers
        // browse or submit applications.
        private readonly VolunteerOpportunityService _opportunityService;

        // Stores applications currently managed by this service.
        private readonly List<VolunteerApplication> _applications = new();

        // Creates the application service and requires an opportunity service
        // so applications can be linked to valid volunteer opportunities.
        public VolunteerApplicationService(
            VolunteerOpportunityService opportunityService)
        {
            _opportunityService =
                opportunityService
                ?? throw new ArgumentNullException(
                    nameof(opportunityService));
        }

        // Returns opportunities that are currently Published and therefore
        // available for volunteers to browse and apply for.
        public IReadOnlyList<VolunteerOpportunity>
            BrowseAvailableOpportunities()
        {
            return _opportunityService.GetPublishedOpportunities();
        }

        // Submits a new application for a volunteer.
        //
        // The method verifies that:
        // - The user exists and has the Volunteer role.
        // - The opportunity exists.
        // - The opportunity is Published.
        // - The volunteer has not already applied to the same opportunity.
        public VolunteerApplication SubmitApplication(
            User volunteer,
            Guid opportunityId)
        {
            if (volunteer == null)
            {
                throw new ArgumentNullException(nameof(volunteer));
            }

            if (volunteer.Role != Role.Volunteer)
            {
                throw new UnauthorizedAccessException(
                    "Only volunteers can submit applications.");
            }

            VolunteerOpportunity? opportunity =
                _opportunityService.FindOpportunityById(
                    opportunityId);

            if (opportunity == null)
            {
                throw new KeyNotFoundException(
                    "Volunteer opportunity was not found.");
            }

            if (opportunity.Status != OpportunityStatus.Published)
            {
                throw new InvalidOperationException(
                    "Applications can only be submitted " +
                    "for published opportunities.");
            }

            bool alreadyApplied =
                _applications.Any(application =>
                    application.VolunteerId == volunteer.Id &&
                    application.OpportunityId == opportunityId);

            if (alreadyApplied)
            {
                throw new InvalidOperationException(
                    "Volunteer has already applied " +
                    "for this opportunity.");
            }

            VolunteerApplication application =
                VolunteerApplication.Create(
                    volunteer.Id,
                    opportunityId);

            _applications.Add(application);

            return application;
        }

        // Returns all applications currently stored in the service.
        public IReadOnlyList<VolunteerApplication>
            GetAllApplications()
        {
            return _applications.AsReadOnly();
        }

        // Searches for an application using its unique application ID.
        // Returns null when the application cannot be found.
        public VolunteerApplication? FindApplicationById(
            Guid applicationId)
        {
            return _applications.FirstOrDefault(
                application =>
                    application.Id == applicationId);
        }

        // Returns only applications belonging to a specific volunteer.
        public IReadOnlyList<VolunteerApplication>
            GetApplicationsForVolunteer(Guid volunteerId)
        {
            if (volunteerId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Volunteer id is required.",
                    nameof(volunteerId));
            }

            return _applications
                .Where(application =>
                    application.VolunteerId == volunteerId)
                .ToList()
                .AsReadOnly();
        }

        // Returns the current status of one application.
        //
        // Only volunteers are allowed to use this operation, and they may
        // only view applications that belong to their own account.
        public VolunteerApplicationStatus GetApplicationStatus(
            User volunteer,
            Guid applicationId)
        {
            if (volunteer == null)
            {
                throw new ArgumentNullException(nameof(volunteer));
            }

            if (volunteer.Role != Role.Volunteer)
            {
                throw new UnauthorizedAccessException(
                    "Only volunteers can view " +
                    "their application status.");
            }

            VolunteerApplication? application =
                FindApplicationById(applicationId);

            if (application == null)
            {
                throw new KeyNotFoundException(
                    "Volunteer application was not found.");
            }

            if (application.VolunteerId != volunteer.Id)
            {
                throw new UnauthorizedAccessException(
                    "Volunteers can only view " +
                    "their own application status.");
            }

            return application.Status;
        }
    }
}