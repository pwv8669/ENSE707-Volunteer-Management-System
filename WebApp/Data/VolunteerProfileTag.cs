namespace WebApp.Data
{
    // Which list a profile tag belongs to.
    public enum ProfileTagKind
    {
        Skill,
        Interest
    }

    // One skill or interest on a volunteer's profile, saved in the database.
    // A separate table (rather than a column on the user) keeps the data easy to
    // search later, e.g. "find volunteers with First aid".
    public class VolunteerProfileTag
    {
        public Guid Id { get; set; }

        // The Identity user (volunteer) this tag belongs to.
        public string UserId { get; set; } = string.Empty;

        public ProfileTagKind Kind { get; set; }

        // The tag text, already cleaned by ProfileTagRules.
        public string Value { get; set; } = string.Empty;

        // Keeps tags in the order the volunteer added them.
        public int SortOrder { get; set; }
    }
}
