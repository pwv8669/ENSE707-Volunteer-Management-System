using System.Security.Claims;
using Volunteer_Management_System;

namespace WebApplication.Services
{
    // Lightweight authentication service used by the custom AuthenticationStateProvider.
    public class AuthService
    {
        // Current user is stored per-scope (scoped DI) so it is tied to a connection.
        public User? CurrentUser { get; private set; }

        public event Action? AuthenticationStateChanged;

        public void SignIn(User user)
        {
            CurrentUser = user;
            AuthenticationStateChanged?.Invoke();
        }

        public void SignOut()
        {
            CurrentUser = null;
            AuthenticationStateChanged?.Invoke();
        }
    }
}
