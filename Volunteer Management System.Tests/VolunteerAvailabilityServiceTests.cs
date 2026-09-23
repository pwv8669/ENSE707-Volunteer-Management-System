
// Purpose:
// Contains unit tests for VolunteerAvailabilityService.
//
// These tests verify that volunteers can add, view, update and remove their
// availability. They also verify role restrictions, ownership rules and
// checks for whether a volunteer is available during a specified time.


using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    // Tests the business rules provided by VolunteerAvailabilityService.
    [TestClass]
    public class VolunteerAvailabilityServiceTests
    {
        // Creates a Volunteer user for availability-related tests.
        private static User CreateVolunteer(
            string username = "volunteer")
        {
            return User.Create(
                username,
                $"{username}@example.com",
                Role.Volunteer);
        }

        // Creates a Coordinator user for testing role restrictions.
        private static User CreateCoordinator()
        {
            return User.Create(
                "coordinator",
                "coordinator@example.com",
                Role.Coordinator);
        }

        // Verifies that a Volunteer can add a new availability period
        // and retrieve it from the service.
        [TestMethod]
        public void AddAvailability_WithVolunteer_StoresAvailability()
        {
            VolunteerAvailabilityService service = new();

            User volunteer =
                CreateVolunteer();

            DateTime from =
                DateTime.UtcNow.AddDays(5);

            VolunteerAvailability availability =
                service.AddAvailability(
                    volunteer,
                    from,
                    from.AddHours(6));

            var results =
                service.GetAvailabilityForVolunteer(
                    volunteer.Id);

            Assert.HasCount(1, results);

            Assert.AreEqual(
                availability.Id,
                results[0].Id);
        }

        // Verifies that a Coordinator cannot manage volunteer availability.
        [TestMethod]
        public void AddAvailability_WithCoordinator_ThrowsUnauthorizedException()
        {
            VolunteerAvailabilityService service = new();

            User coordinator =
                CreateCoordinator();

            DateTime from =
                DateTime.UtcNow.AddDays(5);

            Assert.ThrowsExactly<UnauthorizedAccessException>(() =>
                service.AddAvailability(
                    coordinator,
                    from,
                    from.AddHours(5)));
        }

        // Verifies that availability checking returns true when an existing
        // availability period fully covers the requested event time.
        [TestMethod]
        public void IsVolunteerAvailable_WhenWindowCoversEvent_ReturnsTrue()
        {
            VolunteerAvailabilityService service = new();

            User volunteer =
                CreateVolunteer();

            DateTime from =
                DateTime.UtcNow.AddDays(5);

            service.AddAvailability(
                volunteer,
                from,
                from.AddHours(8));

            bool result =
                service.IsVolunteerAvailable(
                    volunteer.Id,
                    from.AddHours(2),
                    from.AddHours(5));

            Assert.IsTrue(result);
        }

        // Verifies that availability checking returns false when none of
        // the volunteer's availability periods cover the requested event.
        [TestMethod]
        public void IsVolunteerAvailable_WhenNoWindowCoversEvent_ReturnsFalse()
        {
            VolunteerAvailabilityService service = new();

            User volunteer =
                CreateVolunteer();

            DateTime from =
                DateTime.UtcNow.AddDays(5);

            service.AddAvailability(
                volunteer,
                from,
                from.AddHours(2));

            bool result =
                service.IsVolunteerAvailable(
                    volunteer.Id,
                    from.AddHours(3),
                    from.AddHours(5));

            Assert.IsFalse(result);
        }

        // Verifies that a Volunteer can update the start and end times
        // of an availability period that belongs to them.
        [TestMethod]
        public void UpdateAvailability_WithOwnAvailability_UpdatesTimes()
        {
            VolunteerAvailabilityService service = new();

            User volunteer =
                CreateVolunteer();

            DateTime originalFrom =
                DateTime.UtcNow.AddDays(5);

            VolunteerAvailability availability =
                service.AddAvailability(
                    volunteer,
                    originalFrom,
                    originalFrom.AddHours(3));

            DateTime updatedFrom =
                originalFrom.AddHours(1);

            DateTime updatedTo =
                originalFrom.AddHours(8);

            service.UpdateAvailability(
                volunteer,
                availability.Id,
                updatedFrom,
                updatedTo);

            Assert.AreEqual(
                updatedFrom,
                availability.AvailableFrom);

            Assert.AreEqual(
                updatedTo,
                availability.AvailableTo);
        }

        // Verifies that a Volunteer can remove an availability period
        // belonging to their own account.
        [TestMethod]
        public void RemoveAvailability_WithOwnAvailability_RemovesAvailability()
        {
            VolunteerAvailabilityService service = new();

            User volunteer =
                CreateVolunteer();

            DateTime from =
                DateTime.UtcNow.AddDays(5);

            VolunteerAvailability availability =
                service.AddAvailability(
                    volunteer,
                    from,
                    from.AddHours(5));

            bool removed =
                service.RemoveAvailability(
                    volunteer,
                    availability.Id);

            Assert.IsTrue(removed);

            Assert.HasCount(
                0,
                service.GetAvailabilityForVolunteer(
                    volunteer.Id));
        }

        // Verifies that one Volunteer cannot update an availability record
        // belonging to another Volunteer.
        [TestMethod]
        public void UpdateAvailability_WithAnotherVolunteer_ThrowsUnauthorizedException()
        {
            VolunteerAvailabilityService service = new();

            User firstVolunteer =
                CreateVolunteer("volunteer1");

            User secondVolunteer =
                CreateVolunteer("volunteer2");

            DateTime from =
                DateTime.UtcNow.AddDays(5);

            VolunteerAvailability availability =
                service.AddAvailability(
                    firstVolunteer,
                    from,
                    from.AddHours(5));

            Assert.ThrowsExactly<UnauthorizedAccessException>(() =>
                service.UpdateAvailability(
                    secondVolunteer,
                    availability.Id,
                    from,
                    from.AddHours(8)));
        }
    }
}
