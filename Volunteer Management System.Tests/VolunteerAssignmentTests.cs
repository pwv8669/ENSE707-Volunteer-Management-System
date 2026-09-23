
// Purpose:
// Contains unit tests for the VolunteerAssignment model.
//
// These tests verify that assignments are created correctly, required IDs
// are validated, assignments begin with Assigned status, and assignments
// can be cancelled only once.


using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    // Tests the core behaviour of the VolunteerAssignment model.
    [TestClass]
    public class VolunteerAssignmentTests
    {
        // Verifies that valid assignment information creates an assignment
        // with the correct volunteer, opportunity, application and reviewer.
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

        // Verifies that an assignment cannot be created without
        // a valid volunteer ID.
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

        // Verifies that an active assignment can be cancelled and its
        // status changes from Assigned to Cancelled.
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

        // Verifies that an assignment that has already been cancelled
        // cannot be cancelled a second time.
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