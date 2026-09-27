namespace WebApp.Data
{
    // Central role names used by ASP.NET Core Identity and authorization checks.
    public static class AppRoles
    {
        public const string Volunteer = "Volunteer";
        public const string Coordinator = "Coordinator";
        public const string OrganisationAdministrator = "OrganisationAdministrator";

        // Role seeding uses this list to ensure every supported role exists.
        public static IReadOnlyList<string> All { get; } =
        [
            Volunteer,
            Coordinator,
            OrganisationAdministrator
        ];
    }
}
