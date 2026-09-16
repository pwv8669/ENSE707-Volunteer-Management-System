using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class VolunteerAssignmentTests
    {
        [TestMethod]
        public void Create_WithValidDetails_CreatesAssignedAssignment()
        {
            Guid volunteerId = Guid.NewGuid();
            Guid opportunityId = Guid.NewGuid();
            Guid applicationId = Guid.NewGuid();
            Guid coordinatorId = Guid.NewGuid();

            VolunteerAssignment assignment =
                VolunteerAssignment.Create(
                    volunteerId,
                    opportunityId,
                    applicationId,
                    coordinatorId);

            Assert.AreNotEqual(
                Guid.Empty,
                assignment.Id);

            Assert.AreEqual(
                volunteerId,
                assignment.VolunteerId);

            Assert.AreEqual(
                opportunityId,
                assignment.OpportunityId);

            Assert.AreEqual(
                applicationId,
                assignment.ApplicationId);

            Assert.AreEqual(
                coordinatorId,
                assignment.AssignedByUserId);

            Assert.AreEqual(
                AssignmentStatus.Assigned,
                assignment.Status);
        }

        [TestMethod]
        public void Create_WithEmptyVolunteerId_ThrowsException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                VolunteerAssignment.Create(
                    Guid.Empty,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid()));
        }

        [TestMethod]
        public void Cancel_WithAssignedAssignment_SetsCancelledStatus()
        {
            VolunteerAssignment assignment =
                VolunteerAssignment.Create(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid());

            assignment.Cancel();

            Assert.AreEqual(
                AssignmentStatus.Cancelled,
                assignment.Status);
        }

        [TestMethod]
        public void Cancel_WithAlreadyCancelledAssignment_ThrowsException()
        {
            VolunteerAssignment assignment =
                VolunteerAssignment.Create(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid());

            assignment.Cancel();

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                assignment.Cancel());
        }
    }
}