using System;

namespace Volunteer_Management_System
{
    public class VolunteerAvailability
    {
        public Guid Id { get; init; }

        public Guid VolunteerId { get; init; }

        public DateTime AvailableFrom { get; private set; }

        public DateTime AvailableTo { get; private set; }

        public DateTime CreatedAt { get; init; }

        private VolunteerAvailability()
        {
        }

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

        public bool Covers(
            DateTime startDateTime,
            DateTime endDateTime)
        {
            return startDateTime >= AvailableFrom &&
                   endDateTime <= AvailableTo;
        }

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