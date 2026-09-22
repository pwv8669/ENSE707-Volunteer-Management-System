using Volunteer_Management_System;

namespace WebApplication.Services
{
    public interface IUserRepository
    {
        User? FindByEmail(string email);
        bool Add(User user, string password);
        bool VerifyPassword(User user, string password);
    }
}
