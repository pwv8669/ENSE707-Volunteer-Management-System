using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class VolunteerApplicationTests
    {
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

        [TestMethod]
        public void Create_WithEmptyVolunteerId_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                VolunteerApplication.Create(
                    Guid.Empty,
                    Guid.NewGuid()));
        }

        [TestMethod]
        public void Create_WithEmptyOpportunityId_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                VolunteerApplication.Create(
                    Guid.NewGuid(),
                    Guid.Empty));
        }

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