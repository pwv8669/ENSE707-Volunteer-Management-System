namespace WebApp.Services.Profile
{
    // Everything the profile page shows about the signed-in volunteer, in one read-only snapshot.
    public sealed class VolunteerProfileModel
    {
        // The Identity user id (a string) and the same id as a Guid, which the domain services use.
        public required string UserId { get; init; }

        public Guid VolunteerId { get; init; }

        public string Email { get; init; } = string.Empty;

        public required PersonalDetailsInput PersonalDetails { get; init; }

        public IReadOnlyList<string> Skills { get; init; } = [];

        public IReadOnlyList<string> Interests { get; init; } = [];

        // Availability windows, earliest first.
        public IReadOnlyList<AvailabilitySlotModel> Availability { get; init; } = [];

        // How much of the profile is filled in, from 0 to 100.
        public int CompletionPercent { get; init; }

        // "First Last" if a name has been entered, otherwise the email address.
        public string DisplayName
        {
            get
            {
                string fullName = $"{PersonalDetails.FirstName} {PersonalDetails.LastName}".Trim();
                return fullName.Length > 0 ? fullName : Email;
            }
        }

        // Up to two letters for the avatar circle, e.g. "Jane Smith" gives "JS".
        public string Initials
        {
            get
            {
                string first = PersonalDetails.FirstName;
                string last = PersonalDetails.LastName;

                if (first.Length > 0 || last.Length > 0)
                {
                    return $"{FirstLetter(first)}{FirstLetter(last)}".ToUpperInvariant();
                }

                return FirstLetter(Email).ToUpperInvariant();
            }
        }

        private static string FirstLetter(string value) =>
            value.Length > 0 ? value[..1] : string.Empty;
    }

    // One availability window as shown on the page.
    public sealed record AvailabilitySlotModel(Guid Id, DateTime StartsAt, DateTime EndsAt, string? Note)
    {
        // True once the window has finished.
        public bool IsPast(DateTime now) => EndsAt <= now;
    }
}
