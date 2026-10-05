
// Purpose:
// Connects the ASP.NET Core Identity user used by the WebApp with the User
// model used by the Volunteer Management System business logic.
//
// Features 3 and 4 require the core User model when submitting applications,
// managing availability and assigning volunteers. This service converts the
// currently logged-in WebApp account into that domain User model while
// keeping the same user ID.


using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Volunteer_Management_System;
using WebApp.Data;

namespace WebApp.Services
{
    // Provides a bridge between ASP.NET Core Identity users and the
    // Volunteer Management System domain User model.
    public class DomainUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        // Receives ASP.NET Core Identity's UserManager so information about
        // the currently logged-in WebApp user can be retrieved.
        public DomainUserService(
            UserManager<ApplicationUser> userManager)
        {
            _userManager =
                userManager
                ?? throw new ArgumentNullException(
                    nameof(userManager));
        }

        // Retrieves the currently logged-in Identity user and converts it
        // into the User model required by the core business services.
        //
        // The same user ID is preserved so applications, availability and
        // assignments can all be linked to the correct WebApp account.
        public async Task<User> GetCurrentUserAsync(
            ClaimsPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(
                    nameof(principal));
            }

            ApplicationUser? identityUser =
                await _userManager.GetUserAsync(
                    principal);

            if (identityUser == null)
            {
                throw new UnauthorizedAccessException(
                    "A logged-in user could not be found.");
            }

            // ASP.NET Identity stores its user ID as a string.
            // The core Volunteer Management System uses Guid IDs, so the
            // Identity ID is converted before creating the domain User.
            if (!Guid.TryParse(
                identityUser.Id,
                out Guid userId))
            {
                throw new InvalidOperationException(
                    "The Identity user ID is not a valid Guid.");
            }

            string email =
                identityUser.Email
                ?? throw new InvalidOperationException(
                    "The logged-in user does not have an email address.");

            string username =
                identityUser.UserName
                ?? email;

            // Retrieves any ASP.NET Identity roles currently assigned
            // to the logged-in user.
            IList<string> identityRoles =
                await _userManager.GetRolesAsync(
                    identityUser);

            Role domainRole =
                DetermineDomainRole(
                    identityRoles);

            // Uses the User.Create overload added for WebApp integration
            // so the Identity account and domain User share the same ID.
            return User.Create(
                userId,
                username,
                email,
                domainRole);
        }

        // Converts ASP.NET Identity roles into the Role enum used by
        // the Volunteer Management System business logic.
        //
        // Admin has the highest priority, followed by Coordinator.
        // Users without either role are treated as Volunteers.
        private static Role DetermineDomainRole(
            IList<string> roles)
        {
            if (roles.Any(role =>
                role.Equals(
                    AppRoles.OrganisationAdministrator,
                    StringComparison.OrdinalIgnoreCase)))
            {
                return Role.Admin;
            }

            if (roles.Any(role =>
                role.Equals(
                    AppRoles.Coordinator,
                    StringComparison.OrdinalIgnoreCase)))
            {
                return Role.Coordinator;
            }

            return Role.Volunteer;
        }
    }
}
