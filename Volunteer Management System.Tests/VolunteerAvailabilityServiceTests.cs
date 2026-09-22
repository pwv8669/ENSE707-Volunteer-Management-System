using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class VolunteerAvailabilityServiceTests
    {
        private static User CreateVolunteer(
            string username = "volunteer")
        {
            return User.Create(
                username,
                $"{username}@example.com",
                Role.Volunteer);
        }

        private static User CreateCoordinator()
        {
            return User.Create(
                "coordinator",
                "coordinator@example.com",
                Role.Coordinator);
        }

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
