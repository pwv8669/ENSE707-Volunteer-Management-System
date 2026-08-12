using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volunteer_Management_System;

namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class UserTests
    {
        [TestMethod]
        public void Create_ValidInputs_SetsIdentityRoleAndPassword()
        {
            // Arrange
            var username = "  Alice  ";
            var email = "ALICE@Example.COM";
            var password = "StrongPass123!";
            var role = Role.Coordinator;

            // Act
            var user = User.Create(username, email, password, role);

            // Assert
            Assert.AreNotEqual(Guid.Empty, user.Id, "Id should be generated.");
            Assert.AreEqual("Alice", user.Username, "Username should be trimmed.");
            Assert.AreEqual("alice@example.com", user.Email, "Email should be lower-cased and trimmed.");
            Assert.AreEqual(role, user.Role, "Role should be set from factory.");
            Assert.IsTrue((DateTime.UtcNow - user.CreatedAt) < TimeSpan.FromSeconds(5), "CreatedAt should be set to current UTC time.");
            Assert.IsFalse(string.IsNullOrEmpty(user.PasswordSalt), "PasswordSalt must be populated.");
            Assert.IsFalse(string.IsNullOrEmpty(user.PasswordHash), "PasswordHash must be populated.");
            Assert.IsTrue(user.VerifyPassword(password), "VerifyPassword must return true for the original password.");
        }

        [TestMethod]
        public void Create_MissingUsername_Throws()
        {
            try
            {
                User.Create("   ", "a@b.com", "password123", Role.Volunteer);
                Assert.Fail("Expected ArgumentException when username is missing.");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("username", ex.ParamName);
            }
        }

        [TestMethod]
        public void Create_MissingEmail_Throws()
        {
            try
            {
                User.Create("bob", "   ", "password123", Role.Volunteer);
                Assert.Fail("Expected ArgumentException when email is missing.");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("email", ex.ParamName);
            }
        }

        [TestMethod]
        public void Create_ShortPassword_Throws()
        {
            try
            {
                User.Create("bob", "b@c.com", "short", Role.Volunteer);
                Assert.Fail("Expected ArgumentException when password is too short.");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("password", ex.ParamName);
            }
        }

        [TestMethod]
        public void SetPassword_Invalid_Throws()
        {
            var user = (User)Activator.CreateInstance(typeof(User), true);

            try
            {
                user.SetPassword(null);
                Assert.Fail("Expected ArgumentException when setting null password.");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("password", ex.ParamName);
            }

            try
            {
                user.SetPassword("123");
                Assert.Fail("Expected ArgumentException when setting too-short password.");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("password", ex.ParamName);
            }
        }

        [TestMethod]
        public void VerifyPassword_WrongPassword_ReturnsFalse()
        {
            var user = User.Create("joe", "joe@example.com", "CorrectHorseBattery1", Role.Volunteer);

            Assert.IsFalse(user.VerifyPassword("incorrect-password"), "VerifyPassword should return false for wrong password.");
        }

        [TestMethod]
        public void VerifyPassword_NoPasswordSet_ReturnsFalse()
        {
            var user = (User)Activator.CreateInstance(typeof(User), true); // invoke private parameterless ctor
            Assert.IsFalse(user.VerifyPassword("any"), "VerifyPassword should return false when no salt/hash present.");
        }

        [TestMethod]
        public void SetPassword_GeneratesNewSaltAndHash_EachCall()
        {
            var user = User.Create("sara", "sara@example.com", "Password!234", Role.Volunteer);
            var firstSalt = user.PasswordSalt;
            var firstHash = user.PasswordHash;

            // Small pause to avoid any potential timing-based collisions (defensive)
            Thread.Sleep(10);

            user.SetPassword("Password!234");
            var secondSalt = user.PasswordSalt;
            var secondHash = user.PasswordHash;

            Assert.AreNotEqual(firstSalt, secondSalt, "Salt should differ after resetting the password.");
            Assert.AreNotEqual(firstHash, secondHash, "Hash should differ after resetting the password even for same plaintext.");
            Assert.IsTrue(user.VerifyPassword("Password!234"), "VerifyPassword must return true after resetting password.");
        }

        [TestMethod]
        public void UpdateLastLogin_SetsUtcTime()
        {
            var user = User.Create("tim", "tim@example.com", "SomePass123", Role.Volunteer);

            Assert.IsNull(user.LastLoginAt, "LastLoginAt should be null initially.");
            user.UpdateLastLogin();
            Assert.IsNotNull(user.LastLoginAt, "LastLoginAt should be set after UpdateLastLogin.");
            Assert.IsTrue((DateTime.UtcNow - user.LastLoginAt.Value) < TimeSpan.FromSeconds(5), "LastLoginAt should be set to a recent UTC time.");
        }

        [TestMethod]
        public void SetRole_ChangesRole()
        {
            var user = User.Create("kate", "kate@example.com", "PasswordABC", Role.Volunteer);
            user.SetRole(Role.Admin);
            Assert.AreEqual(Role.Admin, user.Role, "SetRole should update the user's role.");
        }
    }
}
