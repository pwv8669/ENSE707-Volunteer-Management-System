using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

// This class contains unit tests for the User class in the Volunteer Management System.
namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class UserTests
    {
        // Test that creating a user with valid inputs sets the identity and role correctly.
        [TestMethod]
        public void Create_ValidInputs_SetsIdentityAndRole()
        {
            // Arrange
            var username = "  Alice  ";
            var email = "ALICE@Example.COM";
            var role = Role.Coordinator;

            // Act
            var user = User.Create(username, email, role);

            // Assert
            Assert.AreNotEqual(Guid.Empty, user.Id);
            Assert.AreEqual("Alice", user.Username);
            Assert.AreEqual("alice@example.com", user.Email);
            Assert.AreEqual(role, user.Role);
            Assert.IsTrue((DateTime.UtcNow - user.CreatedAt) < TimeSpan.FromSeconds(5));
        }

        // Test that creating a user with a missing username throws an ArgumentException.
        [TestMethod]
        public void Create_MissingUsername_Throws()
        {
            var exception = Assert.ThrowsExactly<ArgumentException>(() =>
                User.Create("   ", "a@b.com", Role.Volunteer));

            Assert.AreEqual("username", exception.ParamName);
        }

        // Test that creating a user with a missing email throws an ArgumentException.
        [TestMethod]
        public void Create_MissingEmail_Throws()
        {
            var exception = Assert.ThrowsExactly<ArgumentException>(() =>
                User.Create("bob", "   ", Role.Volunteer));

            Assert.AreEqual("email", exception.ParamName);
        }

        // Test that setting the role updates the user's role correctly.
        [TestMethod]
        public void SetRole_ChangesRole()
        {
            var user = User.Create("kate", "kate@example.com", Role.Volunteer);

            user.SetRole(Role.Admin);

            Assert.AreEqual(Role.Admin, user.Role);
        }
    }
}
