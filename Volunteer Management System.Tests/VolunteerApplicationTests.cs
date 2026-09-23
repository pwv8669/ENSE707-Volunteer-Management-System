
// Purpose:
// Contains unit tests for the VolunteerApplication model.
//
// These tests verify application creation, required IDs, approval and
// rejection behaviour, review information, and protection against reviewing
// an application more than once.


using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    // Tests the core behaviour of the VolunteerApplication model.
    [TestClass]
    public class VolunteerApplicationTests
    {
        // Verifies that valid volunteer and opportunity IDs create a new
        // Pending application without review information.
        [TestMethod]
        public void Create_WithValidIds_CreatesPendingApplication()
        {
            Guid volunteerId = Guid.NewGuid();
            Guid opportunityId = Guid.NewGuid();

            VolunteerApplication application =
                VolunteerApplication.Create(
                    volunteerId,
                    opportunityId);

            Assert.AreNotEqual(
                Guid.Empty,
                application.Id);

            Assert.AreEqual(
                volunteerId,
                application.VolunteerId);

            Assert.AreEqual(
                opportunityId,
                application.OpportunityId);

            Assert.AreEqual(
                VolunteerApplicationStatus.Pending,
                application.Status);

            Assert.IsNull(application.ReviewedAt);
            Assert.IsNull(application.ReviewedByUserId);
        }

        // Verifies that an application cannot be created without a valid
        // volunteer ID.
        [TestMethod]
        public void Create_WithEmptyVolunteerId_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                VolunteerApplication.Create(
                    Guid.Empty,
                    Guid.NewGuid()));
        }

        // Verifies that an application cannot be created without a valid
        // opportunity ID.
        [TestMethod]
        public void Create_WithEmptyOpportunityId_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                VolunteerApplication.Create(
                    Guid.NewGuid(),
                    Guid.Empty));
        }

        // Verifies that approving a Pending application changes its status
        // and stores the reviewer information.
        [TestMethod]
        public void Approve_WithPendingApplication_SetsApprovedStatus()
        {
            VolunteerApplication application =
                VolunteerApplication.Create(
                    Guid.NewGuid(),
                    Guid.NewGuid());

            Guid reviewerId = Guid.NewGuid();

            application.Approve(reviewerId);

            Assert.AreEqual(
                VolunteerApplicationStatus.Approved,
                application.Status);

            Assert.AreEqual(
                reviewerId,
                application.ReviewedByUserId);

            Assert.IsNotNull(
                application.ReviewedAt);
        }

        // Verifies that rejecting a Pending application changes its status
        // and stores the reviewer information.
        [TestMethod]
        public void Reject_WithPendingApplication_SetsRejectedStatus()
        {
            VolunteerApplication application =
                VolunteerApplication.Create(
                    Guid.NewGuid(),
                    Guid.NewGuid());

            Guid reviewerId = Guid.NewGuid();

            application.Reject(reviewerId);

            Assert.AreEqual(
                VolunteerApplicationStatus.Rejected,
                application.Status);

            Assert.AreEqual(
                reviewerId,
                application.ReviewedByUserId);

            Assert.IsNotNull(
                application.ReviewedAt);
        }

        // Verifies that an application that has already been reviewed cannot
        // be approved or reviewed again.
        [TestMethod]
        public void Approve_WithAlreadyReviewedApplication_ThrowsException()
        {
            VolunteerApplication application =
                VolunteerApplication.Create(
                    Guid.NewGuid(),
                    Guid.NewGuid());

            application.Reject(Guid.NewGuid());

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                application.Approve(Guid.NewGuid()));
        }
    }
}