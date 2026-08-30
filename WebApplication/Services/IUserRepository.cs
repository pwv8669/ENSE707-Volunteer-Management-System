using Volunteer_Management_System;

namespace WebApplication.Services
{
    public interface IUserRepository
    {
        User? FindByEmail(string email);
        void Add(User user);
    }
}
