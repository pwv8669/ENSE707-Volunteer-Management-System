
// Purpose:
// Provides the main operations for managing volunteer availability.
//
// This service allows volunteers to add, view, update and remove their
// availability. It also checks whether a volunteer is available for the
// full duration of a volunteer opportunity.
//
// The service applies access rules so that only Volunteer users can manage
// availability and volunteers can only update or remove their own records.


using System;
using System.Collections.Generic;
using System.Linq;

namespace Volunteer_Management_System
{
    // Handles creation, retrieval, updating and removal of volunteer
    // availability records.
    public class VolunteerAvailabilityService
    {
        // Stores availability records currently managed by this service.
        private readonly List<VolunteerAvailability> _availability = new();

        // Adds a new availability period for a Volunteer user.
        // The volunteer role is validated before the availability record
        // is created and stored.
        public VolunteerAvailability AddAvailability(
            User volunteer,
            DateTime availableFrom,
            DateTime availableTo)
        {
            ValidateVolunteer(volunteer);

            VolunteerAvailability availability =
                VolunteerAvailability.Create(
                    volunteer.Id,
                    availableFrom,
                    availableTo);

            _availability.Add(availability);

            return availability;
        }

        // Returns all availability periods belonging to a specific volunteer.
        // The results are ordered by their starting date and time.
        public IReadOnlyList<VolunteerAvailability>
            GetAvailabilityForVolunteer(Guid volunteerId)
        {
            if (volunteerId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Volunteer id is required.",
                    nameof(volunteerId));
            }

            return _availability
                .Where(item =>
                    item.VolunteerId == volunteerId)
                .OrderBy(item =>
                    item.AvailableFrom)
                .ToList()
                .AsReadOnly();
        }

        // Checks whether a volunteer has an availability period that fully
        // covers the requested start and end time.
        public bool IsVolunteerAvailable(
            Guid volunteerId,
            DateTime startDateTime,
            DateTime endDateTime)
        {
            if (volunteerId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Volunteer id is required.",
                    nameof(volunteerId));
            }

            if (endDateTime <= startDateTime)
            {
                throw new ArgumentException(
                    "End time must be after start time.",
                    nameof(endDateTime));
            }

            return _availability.Any(item =>
                item.VolunteerId == volunteerId &&
                item.Covers(
                    startDateTime,
                    endDateTime));
        }

        // Updates one of the volunteer's existing availability periods.
        //
        // Volunteers can only update availability records that belong
        // to their own user account.
        public void UpdateAvailability(
            User volunteer,
            Guid availabilityId,
            DateTime availableFrom,
            DateTime availableTo)
        {
            ValidateVolunteer(volunteer);

            VolunteerAvailability? availability =
                _availability.FirstOrDefault(item =>
                    item.Id == availabilityId);

            if (availability == null)
            {
                throw new KeyNotFoundException(
                    "Volunteer availability was not found.");
            }

            if (availability.VolunteerId != volunteer.Id)
            {
                throw new UnauthorizedAccessException(
                    "Volunteers can only update their own availability.");
            }

            availability.Update(
                availableFrom,
                availableTo);
        }

        // Removes an availability period belonging to the volunteer.
        //
        // Returns false if the availability record does not exist.
        // Volunteers cannot remove another volunteer's availability.
        public bool RemoveAvailability(
            User volunteer,
            Guid availabilityId)
        {
            ValidateVolunteer(volunteer);

            VolunteerAvailability? availability =
                _availability.FirstOrDefault(item =>
                    item.Id == availabilityId);

            if (availability == null)
            {
                return false;
            }

            if (availability.VolunteerId != volunteer.Id)
            {
                throw new UnauthorizedAccessException(
                    "Volunteers can only remove their own availability.");
            }

            return _availability.Remove(availability);
        }

        // Ensures that the supplied user exists and has the Volunteer role
        // before allowing availability management operations.
        private static void ValidateVolunteer(
            User volunteer)
        {
            if (volunteer == null)
            {
                throw new ArgumentNullException(
                    nameof(volunteer));
            }

            if (volunteer.Role != Role.Volunteer)
            {
                throw new UnauthorizedAccessException(
                    "Only volunteers can manage availability.");
            }
        }
    }
}