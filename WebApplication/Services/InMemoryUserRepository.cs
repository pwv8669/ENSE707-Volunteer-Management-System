using System.Collections.Concurrent;
using Microsoft.AspNetCore.Identity;
using Volunteer_Management_System;

namespace WebApplication.Services
{
    public class InMemoryUserRepository : IUserRepository
    {
        // Keep hashes in this temporary store; User only carries identity and role data.
        private readonly ConcurrentDictionary<string, (User User, string PasswordHash)> _byEmail = new(StringComparer.OrdinalIgnoreCase);
        private readonly PasswordHasher<User> _passwordHasher = new();

        private static string NormalizeEmail(string? email) => string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();

        public InMemoryUserRepository()
        {
            // Seed a test user: password = TestPass123
            var u = User.Create("test", "test@example.com", Role.Volunteer);
            Add(u, "TestPass123");
        }

        public User? FindByEmail(string email)
        {
            var key = NormalizeEmail(email);
            if (string.IsNullOrEmpty(key)) return null;
            return _byEmail.TryGetValue(key, out var entry) ? entry.User : null;
        }

        public bool Add(User user, string password)
        {
            ArgumentNullException.ThrowIfNull(user);
            var key = NormalizeEmail(user.Email);
            if (string.IsNullOrEmpty(key)) return false;

            var passwordHash = _passwordHasher.HashPassword(user, password);
            return _byEmail.TryAdd(key, (user, passwordHash));
        }

        public bool VerifyPassword(User user, string password)
        {
            ArgumentNullException.ThrowIfNull(user);
            var key = NormalizeEmail(user.Email);
            return _byEmail.TryGetValue(key, out var entry)
                && ReferenceEquals(entry.User, user)
                && _passwordHasher.VerifyHashedPassword(user, entry.PasswordHash, password)
                    != PasswordVerificationResult.Failed;
        }
    }
}
