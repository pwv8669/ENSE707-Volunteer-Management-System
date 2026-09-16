using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class VolunteerAvailabilityTests
    {
        [TestMethod]
        public void Create_WithValidTimes_CreatesAvailability()
        {
            Guid volunteerId = Guid.NewGuid();

            DateTime from =
                DateTime.UtcNow.AddDays(5);

            DateTime to =
                from.AddHours(5);

            VolunteerAvailability availability =
                VolunteerAvailability.Create(
                    volunteerId,
                    from,
                    to);

            Assert.AreNotEqual(
                Guid.Empty,
                availability.Id);

            Assert.AreEqual(
                volunteerId,
                availability.VolunteerId);

            Assert.AreEqual(
                from,
                availability.AvailableFrom);

            Assert.AreEqual(
                to,
                availability.AvailableTo);
        }

        [TestMethod]
        public void Create_WithEmptyVolunteerId_ThrowsException()
        {
            DateTime from =
                DateTime.UtcNow.AddDays(5);

            Assert.ThrowsExactly<ArgumentException>(() =>
                VolunteerAvailability.Create(
                    Guid.Empty,
                    from,
                    from.AddHours(5)));
        }

        [TestMethod]
        public void Create_WithInvalidTimes_ThrowsException()
        {
            DateTime from =
                DateTime.UtcNow.AddDays(5);

            Assert.ThrowsExactly<ArgumentException>(() =>
                VolunteerAvailability.Create(
                    Guid.NewGuid(),
                    from,
                    from.AddHours(-1)));
        }

        [TestMethod]
        public void Covers_WhenOpportunityInsideAvailability_ReturnsTrue()
        {
            DateTime from =
                DateTime.UtcNow.AddDays(5);

            VolunteerAvailability availability =
                VolunteerAvailability.Create(
                    Guid.NewGuid(),
                    from,
                    from.AddHours(8));

            bool result =
                availability.Covers(
                    from.AddHours(2),
                    from.AddHours(5));

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Covers_WhenOpportunityOutsideAvailability_ReturnsFalse()
        {
            DateTime from =
                DateTime.UtcNow.AddDays(5);

            VolunteerAvailability availability =
                VolunteerAvailability.Create(
                    Guid.NewGuid(),
                    from,
                    from.AddHours(3));

            bool result =
                availability.Covers(
                    from.AddHours(2),
                    from.AddHours(5));

            Assert.IsFalse(result);
        }
    }
}