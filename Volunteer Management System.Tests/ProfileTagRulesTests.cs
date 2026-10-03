using System.Linq;

// This class contains unit tests for the ProfileTagRules class in the Volunteer Management System.
namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class ProfileTagRulesTests
    {
        // Test that normalizing trims the ends and collapses repeated spaces inside a tag.
        [TestMethod]
        public void Normalize_WithExtraSpaces_TrimsAndCollapses()
        {
            Assert.AreEqual("first aid", ProfileTagRules.Normalize("  first   aid "));
        }

        // Test that normalizing null or blank input gives an empty string.
        [TestMethod]
        public void Normalize_WithBlankInput_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, ProfileTagRules.Normalize(null));
            Assert.AreEqual(string.Empty, ProfileTagRules.Normalize("   "));
        }

        // Test that a new, valid tag passes validation.
        [TestMethod]
        public void Validate_WithNewTag_ReturnsNull()
        {
            Assert.IsNull(ProfileTagRules.Validate("Driving", ["First aid"]));
        }

        // Test that a blank tag is rejected.
        [TestMethod]
        public void Validate_WithBlankTag_ReturnsError()
        {
            Assert.IsNotNull(ProfileTagRules.Validate("   ", []));
        }

        // Test that a tag longer than the limit is rejected.
        [TestMethod]
        public void Validate_WithTooLongTag_ReturnsError()
        {
            string tooLong = new('a', ProfileTagRules.MaxTagLength + 1);

            string? error = ProfileTagRules.Validate(tooLong, []);

            Assert.IsNotNull(error);
            StringAssert.Contains(error, ProfileTagRules.MaxTagLength.ToString());
        }

        // Test that a duplicate is rejected even when the upper/lower case differs.
        [TestMethod]
        public void Validate_WithDuplicateDifferentCase_ReturnsError()
        {
            string? error = ProfileTagRules.Validate("FIRST AID", ["First aid"]);

            Assert.IsNotNull(error);
            StringAssert.Contains(error, "already in your list");
        }

        // Test that a tag can't be added once the list is full.
        [TestMethod]
        public void Validate_WhenListIsFull_ReturnsError()
        {
            List<string> full = Enumerable.Range(1, ProfileTagRules.MaxTags)
                .Select(number => $"Skill {number}")
                .ToList();

            string? error = ProfileTagRules.Validate("One more", full);

            Assert.IsNotNull(error);
            StringAssert.Contains(error, ProfileTagRules.MaxTags.ToString());
        }

        // Test that normalizing a list cleans each tag, drops blanks and duplicates, and keeps the order.
        [TestMethod]
        public void TryNormalizeList_WithMessyInput_CleansAndKeepsFirstOfEachDuplicate()
        {
            bool ok = ProfileTagRules.TryNormalizeList(
                [" Cooking ", "", "first  aid", "COOKING", "Driving"],
                out IReadOnlyList<string> tags,
                out string? error);

            Assert.IsTrue(ok);
            Assert.IsNull(error);
            CollectionAssert.AreEqual(
                new[] { "Cooking", "first aid", "Driving" },
                tags.ToArray());
        }

        // Test that a null list is treated as an empty list.
        [TestMethod]
        public void TryNormalizeList_WithNull_ReturnsEmptyList()
        {
            bool ok = ProfileTagRules.TryNormalizeList(null, out IReadOnlyList<string> tags, out _);

            Assert.IsTrue(ok);
            Assert.HasCount(0, tags);
        }

        // Test that a list with more than the allowed number of tags fails with a message.
        [TestMethod]
        public void TryNormalizeList_WithTooManyTags_ReturnsFalse()
        {
            IEnumerable<string> tooMany = Enumerable.Range(1, ProfileTagRules.MaxTags + 1)
                .Select(number => $"Skill {number}");

            bool ok = ProfileTagRules.TryNormalizeList(tooMany, out _, out string? error);

            Assert.IsFalse(ok);
            Assert.IsNotNull(error);
        }

        // Test that a list containing a tag that is too long fails with a message.
        [TestMethod]
        public void TryNormalizeList_WithTooLongTag_ReturnsFalse()
        {
            string tooLong = new('a', ProfileTagRules.MaxTagLength + 1);

            bool ok = ProfileTagRules.TryNormalizeList(["Cooking", tooLong], out _, out string? error);

            Assert.IsFalse(ok);
            StringAssert.Contains(error!, "longer than");
        }
    }
}
