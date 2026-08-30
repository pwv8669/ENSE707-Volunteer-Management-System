using System.Collections.Concurrent;
using Volunteer_Management_System;

namespace WebApplication.Services
{
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly ConcurrentDictionary<string, User> _byEmail = new(StringComparer.OrdinalIgnoreCase);

        private static string NormalizeEmail(string? email) => string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();

        public InMemoryUserRepository()
        {
            // Seed a test user: password = TestPass123
            var u = User.Create("test", "test@example.com", "TestPass123", Role.Volunteer);
            _byEmail.TryAdd(u.Email, u);
        }

        public User? FindByEmail(string email)
        {
            var key = NormalizeEmail(email);
            if (string.IsNullOrEmpty(key)) return null;
            _byEmail.TryGetValue(key, out var user);
            return user;
        }

        public void Add(User user)
        {
            var key = NormalizeEmail(user?.Email);
            if (string.IsNullOrEmpty(key)) return;
            _byEmail.TryAdd(key, user);
        }
    }
}
