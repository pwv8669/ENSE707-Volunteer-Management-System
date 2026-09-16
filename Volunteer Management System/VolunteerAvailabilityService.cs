using System;
using System.Collections.Generic;
using System.Linq;

namespace Volunteer_Management_System
{
    public class VolunteerAvailabilityService
    {
        private readonly List<VolunteerAvailability> _availability = new();

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