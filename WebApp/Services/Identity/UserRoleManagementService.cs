using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;

namespace WebApp.Services.Identity
{
    // Provides administrator-only role queries and changes. Authorization is
    // repeated here so callers cannot bypass it by invoking the service directly.
    public sealed class UserRoleManagementService(
        UserManager<ApplicationUser> userManager)
    {
        public async Task<IReadOnlyList<UserRoleSummary>> GetUsersAsync(
            string actingUserId)
        {
            await RequireAdministratorAsync(actingUserId);

            List<ApplicationUser> users = await userManager.Users
                .OrderBy(user => user.Email)
                .ToListAsync();
            List<UserRoleSummary> summaries = new(users.Count);

            foreach (ApplicationUser user in users)
            {
                IList<string> roles = await userManager.GetRolesAsync(user);
                summaries.Add(new UserRoleSummary(
                    user.Id,
                    user.Email ?? user.UserName ?? "Unknown user",
                    GetDisplayName(user),
                    roles.FirstOrDefault(AppRoles.All.Contains) ?? AppRoles.Volunteer));
            }

            return summaries;
        }

        public async Task<RoleUpdateResult> UpdateRoleAsync(
            string actingUserId,
            string targetUserId,
            string newRole)
        {
            await RequireAdministratorAsync(actingUserId);

            if (!AppRoles.All.Contains(newRole))
            {
                return RoleUpdateResult.Failure("The selected role is invalid.");
            }

            ApplicationUser? targetUser =
                await userManager.FindByIdAsync(targetUserId);
            if (targetUser is null)
            {
                return RoleUpdateResult.Failure("The selected user no longer exists.");
            }

            IList<string> currentRoles = await userManager.GetRolesAsync(targetUser);
            if (currentRoles.Count == 1 && currentRoles[0] == newRole)
            {
                return RoleUpdateResult.Success("The user already has that role.");
            }

            if (currentRoles.Contains(AppRoles.OrganisationAdministrator) &&
                newRole != AppRoles.OrganisationAdministrator)
            {
                IList<ApplicationUser> administrators =
                    await userManager.GetUsersInRoleAsync(
                        AppRoles.OrganisationAdministrator);
                if (administrators.Count <= 1)
                {
                    return RoleUpdateResult.Failure(
                        "The final organisation administrator cannot be demoted.");
                }
            }

            bool addedNewRole = !currentRoles.Contains(newRole);
            if (addedNewRole)
            {
                IdentityResult addResult =
                    await userManager.AddToRoleAsync(targetUser, newRole);
                if (!addResult.Succeeded)
                {
                    return RoleUpdateResult.Failure(FormatErrors(addResult));
                }
            }

            string[] rolesToRemove = currentRoles
                .Where(role => role != newRole)
                .ToArray();
            if (rolesToRemove.Length > 0)
            {
                IdentityResult removeResult =
                    await userManager.RemoveFromRolesAsync(
                        targetUser,
                        rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    // Roll back the newly-added role when the replacement fails.
                    if (addedNewRole)
                    {
                        await userManager.RemoveFromRoleAsync(targetUser, newRole);
                    }

                    return RoleUpdateResult.Failure(FormatErrors(removeResult));
                }
            }

            // Invalidates existing authentication cookies after their next
            // security-stamp check so removed privileges do not persist.
            IdentityResult stampResult =
                await userManager.UpdateSecurityStampAsync(targetUser);
            if (!stampResult.Succeeded)
            {
                return RoleUpdateResult.Failure(FormatErrors(stampResult));
            }

            return RoleUpdateResult.Success(
                $"{targetUser.Email ?? targetUser.UserName ?? "The user"} is now {FormatRole(newRole)}.");
        }

        private async Task RequireAdministratorAsync(string actingUserId)
        {
            ApplicationUser? actingUser =
                await userManager.FindByIdAsync(actingUserId);
            if (actingUser is null ||
                !await userManager.IsInRoleAsync(
                    actingUser,
                    AppRoles.OrganisationAdministrator))
            {
                throw new UnauthorizedAccessException(
                    "Only organisation administrators can manage user roles.");
            }
        }

        private static string GetDisplayName(ApplicationUser user)
        {
            string fullName = $"{user.FirstName} {user.LastName}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? "Not provided" : fullName;
        }

        public static string FormatRole(string role) => role switch
        {
            AppRoles.OrganisationAdministrator => "Organisation Administrator",
            _ => role
        };

        private static string FormatErrors(IdentityResult result) =>
            string.Join(" ", result.Errors.Select(error => error.Description));
    }

    public sealed record UserRoleSummary(
        string Id,
        string Email,
        string DisplayName,
        string Role);

    public sealed record RoleUpdateResult(bool Succeeded, string Message)
    {
        public static RoleUpdateResult Success(string message) => new(true, message);

        public static RoleUpdateResult Failure(string message) => new(false, message);
    }
}
