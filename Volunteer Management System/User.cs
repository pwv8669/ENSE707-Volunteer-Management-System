using System;
// Represents a user for the volunteer management business logic.
// WebApp's ASP.NET Core Identity handles authentication and passwords.
namespace Volunteer_Management_System
{
    // Roles for users in the system.
    public enum Role
    {
        Volunteer,
        Coordinator,
        Admin
    }

    // Represents a user in the Volunteer Management System.
    public class User
    {
        // Identity
        public Guid Id { get; init; }
        public string Username { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;

        // Authorization
        public Role Role { get; private set; }

        // Auditing
        public DateTime CreatedAt { get; init; }

        // Parameterless constructor for ORMs/serializers
        private User() { }

        // Factory for creating a new user
        public static User Create(string username, string email, Role role)
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username is required.", nameof(username));
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));

            return new User
            {
                Id = Guid.NewGuid(),
                Username = username.Trim(),
                Email = email.Trim().ToLowerInvariant(),
                Role = role,
                CreatedAt = DateTime.UtcNow
            };
        }

        // Update role (validation/authorization should be applied by higher layers)
        public void SetRole(Role newRole) => Role = newRole;
    }
}
