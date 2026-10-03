using System;
using System.Collections.Generic;
using System.Linq;

// The purpose of this class is to hold the rules for the short entries
// ("tags") a volunteer adds to their profile, such as skills and interests,
// so the web app and the unit tests share one definition of what is valid.
namespace Volunteer_Management_System
{
    // Validation and clean-up rules for profile tags such as skills and interests.
    public static class ProfileTagRules
    {
        // Most entries a volunteer can have in one list (skills or interests).
        public const int MaxTags = 20;

        // Longest a single entry can be, in characters.
        public const int MaxTagLength = 40;

        // Trims a tag and collapses repeated spaces, e.g. "  first   aid " becomes "first aid".
        // Null or blank input becomes an empty string.
        public static string Normalize(string? tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return string.Empty;
            }

            string[] words = tag.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries);

            return string.Join(' ', words);
        }

        // Checks whether a new tag can be added to an existing list.
        // Returns an error message to show the volunteer, or null when the tag is fine.
        public static string? Validate(string? tag, IEnumerable<string> existingTags)
        {
            ArgumentNullException.ThrowIfNull(existingTags);

            string normalized = Normalize(tag);

            if (normalized.Length == 0)
            {
                return "Enter a value before adding it.";
            }

            if (normalized.Length > MaxTagLength)
            {
                return $"Keep each entry to {MaxTagLength} characters or fewer.";
            }

            List<string> existing = existingTags.ToList();

            // Duplicates are compared case-insensitively, so "First Aid" and "first aid" count as the same.
            if (existing.Any(item => IsSameTag(item, normalized)))
            {
                return $"\"{normalized}\" is already in your list.";
            }

            if (existing.Count >= MaxTags)
            {
                return $"You can add up to {MaxTags} entries.";
            }

            return null;
        }

        // Cleans a whole list before it is saved: normalizes each tag, drops blanks and
        // case-insensitive duplicates (keeping the first one), then checks the limits.
        // Returns false with an error message if a tag is too long or there are too many tags.
        public static bool TryNormalizeList(
            IEnumerable<string>? tags,
            out IReadOnlyList<string> normalizedTags,
            out string? error)
        {
            List<string> result = new();
            normalizedTags = result.AsReadOnly();
            error = null;

            if (tags == null)
            {
                return true;
            }

            foreach (string tag in tags)
            {
                string normalized = Normalize(tag);

                if (normalized.Length == 0)
                {
                    continue;
                }

                if (normalized.Length > MaxTagLength)
                {
                    error = $"\"{normalized}\" is longer than {MaxTagLength} characters.";
                    return false;
                }

                if (result.Any(existing => IsSameTag(existing, normalized)))
                {
                    continue;
                }

                result.Add(normalized);
            }

            if (result.Count > MaxTags)
            {
                error = $"A list can have at most {MaxTags} entries.";
                return false;
            }

            return true;
        }

        // Two tags are the same if they match after normalizing, ignoring upper/lower case.
        private static bool IsSameTag(string first, string second)
        {
            return string.Equals(
                Normalize(first),
                Normalize(second),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
