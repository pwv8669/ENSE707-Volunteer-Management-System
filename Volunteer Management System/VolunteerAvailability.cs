
// Purpose:
// Represents a period of time when a volunteer is available to participate
// in volunteer opportunities.
//
// The class stores the volunteer ID, availability start and end times,
// creation time, and provides methods to check or update availability.


using System;

namespace Volunteer_Management_System
{
    // Represents one availability time period for a volunteer.
    public class VolunteerAvailability
    {
        public Guid Id { get; init; }

        public Guid VolunteerId { get; init; }

        public DateTime AvailableFrom { get; private set; }

        public DateTime AvailableTo { get; private set; }

        public DateTime CreatedAt { get; init; }

        // Private constructor ensures availability records are created
        // through the Create method so the values are validated first.
        private VolunteerAvailability()
        {
        }

        // Creates a new availability period for a volunteer.
        //
        // The method verifies that a valid volunteer ID is provided and
        // that the availability end time occurs after the start time.
        public static VolunteerAvailability Create(
            Guid volunteerId,
            DateTime availableFrom,
            DateTime availableTo)
        {
            if (volunteerId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Volunteer id is required.",
                    nameof(volunteerId));
            }

            if (availableTo <= availableFrom)
            {
                throw new ArgumentException(
                    "Available-to time must be after available-from time.",
                    nameof(availableTo));
            }

            return new VolunteerAvailability
            {
                Id = Guid.NewGuid(),
                VolunteerId = volunteerId,
                AvailableFrom = availableFrom,
                AvailableTo = availableTo,
                CreatedAt = DateTime.UtcNow
            };
        }

        // Checks whether this availability period fully covers a specified
        // opportunity or event time range.
        public bool Covers(
            DateTime startDateTime,
            DateTime endDateTime)
        {
            return startDateTime >= AvailableFrom &&
                   endDateTime <= AvailableTo;
        }

        // Updates an existing availability period.
        // The new end time must occur after the new start time.
        public void Update(
            DateTime availableFrom,
            DateTime availableTo)
        {
            if (availableTo <= availableFrom)
            {
                throw new ArgumentException(
                    "Available-to time must be after available-from time.",
                    nameof(availableTo));
            }

            AvailableFrom = availableFrom;
            AvailableTo = availableTo;
        }
    }
}