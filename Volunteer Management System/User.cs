using System;
using System.Security.Cryptography;
using System.Text;

namespace Volunteer_Management_System
{
    public enum Role
    {
        Volunteer,
        Coordinator,
        Admin
    }

    public class User
    {
        // Identity
        public Guid Id { get; init; }
        public string Username { get; private set; }
        public string Email { get; private set; }

        // Authorization
        public Role Role { get; private set; }

        // Password storage (persist these; keep hashed and salted)
        public string PasswordHash { get; private set; } = string.Empty; // Base64
        public string PasswordSalt { get; private set; } = string.Empty; // Base64

        // Auditing
        public DateTime CreatedAt { get; init; }
        public DateTime? LastLoginAt { get; private set; }

        // Parameterless constructor for ORMs/serializers
        private User() { }

        // Factory for creating a new user with password hashing
        public static User Create(string username, string email, string password, Role role)
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username is required.", nameof(username));
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));
            if (string.IsNullOrEmpty(password)) throw new ArgumentException("Password is required.", nameof(password));
            if (password.Length < 8) throw new ArgumentException("Password must be at least 8 characters.", nameof(password));

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username.Trim(),
                Email = email.Trim().ToLowerInvariant(),
                Role = role,
                CreatedAt = DateTime.UtcNow
            };

            user.SetPassword(password);
            return user;
        }

        // Sets a new password (generates salt + hash)
        public void SetPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentException("Password is required.", nameof(password));
            if (password.Length < 8) throw new ArgumentException("Password must be at least 8 characters.", nameof(password));

            // Strong salt
            var salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // PBKDF2 with SHA-256
            const int iterations = 100_000;
            const int hashByteSize = 32;
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                hashByteSize);

            PasswordSalt = Convert.ToBase64String(salt);
            PasswordHash = Convert.ToBase64String(hash);
        }

        // Verify a plaintext password against stored hash
        public bool VerifyPassword(string password)
        {
            if (string.IsNullOrEmpty(PasswordSalt) || string.IsNullOrEmpty(PasswordHash)) return false;

            var salt = Convert.FromBase64String(PasswordSalt);
            var expectedHash = Convert.FromBase64String(PasswordHash);

            const int iterations = 100_000;
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }

        // Update role (validation/authorization should be applied by higher layers)
        public void SetRole(Role newRole) => Role = newRole;

        // Record successful login
        public void UpdateLastLogin() => LastLoginAt = DateTime.UtcNow;
    }
}
