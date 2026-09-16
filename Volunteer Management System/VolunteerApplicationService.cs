using System;
using System.Collections.Generic;
using System.Linq;

namespace Volunteer_Management_System
{
    public class VolunteerApplicationService
    {
        private readonly VolunteerOpportunityService _opportunityService;

        private readonly List<VolunteerApplication> _applications = new();

        public VolunteerApplicationService(
            VolunteerOpportunityService opportunityService)
        {
            _opportunityService =
                opportunityService
                ?? throw new ArgumentNullException(
                    nameof(opportunityService));
        }

        public IReadOnlyList<VolunteerOpportunity>
            BrowseAvailableOpportunities()
        {
            return _opportunityService.GetPublishedOpportunities();
        }

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

        public IReadOnlyList<VolunteerApplication>
            GetAllApplications()
        {
            return _applications.AsReadOnly();
        }

        public VolunteerApplication? FindApplicationById(
            Guid applicationId)
        {
            return _applications.FirstOrDefault(
                application =>
                    application.Id == applicationId);
        }

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