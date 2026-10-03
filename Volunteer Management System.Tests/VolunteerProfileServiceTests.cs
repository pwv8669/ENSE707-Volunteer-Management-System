using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Data;
using WebApp.Services.Profile;

// This class contains unit tests for the WebApp's VolunteerProfileService, using an in-memory database.
namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class VolunteerProfileServiceTests
    {
        // Every test runs at this fixed "now" (Sunday 5 October 2026, 10:00), so time-based rules are predictable.
        private static readonly DateTime FixedNow = new(2026, 10, 5, 10, 0, 0);

        // Test that a brand-new account loads with an empty profile and 0% completion.
        [TestMethod]
        public async Task GetProfileAsync_NewUser_ReturnsEmptyProfile()
        {
            await using ProfileTestHarness context = await ProfileTestHarness.CreateAsync();

            VolunteerProfileModel? profile = await context.Service.GetProfileAsync(context.UserId);

            Assert.IsNotNull(profile);
            Assert.AreEqual("volunteer@example.com", profile.Email);
            Assert.AreEqual(string.Empty, profile.PersonalDetails.FirstName);
            Assert.HasCount(0, profile.Skills);
            Assert.HasCount(0, profile.Availability);
            Assert.AreEqual(0, profile.CompletionPercent);
            Assert.AreEqual(Guid.Parse(context.UserId), profile.VolunteerId);
        }

        // Test that loading a profile for an account that doesn't exist returns null.
        [TestMethod]
        public async Task GetProfileAsync_UnknownUser_ReturnsNull()
        {
            await using ProfileTestHarness context = await ProfileTestHarness.CreateAsync();

            Assert.IsNull(await context.Service.GetProfileAsync(Guid.NewGuid().ToString()));
        }

        // Test that valid personal details are saved and raise the completion percentage.
        [TestMethod]
        public async Task UpdatePersonalDetailsAsync_ValidInput_SavesDetails()
        {
            await using ProfileTestHarness context = await ProfileTestHarness.CreateAsync();

            ProfileResult result = await context.Service.UpdatePersonalDetailsAsync(
                context.UserId,
                ValidDetails());

            Assert.IsTrue(result.Succeeded, result.Message);
            VolunteerProfileModel profile = (await context.Service.GetProfileAsync(context.UserId))!;
            Assert.AreEqual("Aroha", profile.PersonalDetails.FirstName);
            Assert.AreEqual("Ngata", profile.PersonalDetails.LastName);
            Assert.AreEqual("021 123 4567", profile.PersonalDetails.PhoneNumber);
            Assert.AreEqual("Aroha Ngata", profile.DisplayName);
            Assert.AreEqual(70, profile.CompletionPercent);
        }

        // Test that a missing first name is rejected and nothing is saved.
        [TestMethod]
        public async Task UpdatePersonalDetailsAsync_MissingFirstName_ReturnsFailure()
        {
            await using ProfileTestHarness context = await ProfileTestHarness.CreateAsync();
            PersonalDetailsInput input = ValidDetails();
            input.FirstName = "";

            ProfileResult result = await context.Service.UpdatePersonalDetailsAsync(context.UserId, input);

            Assert.IsFalse(result.Succeeded);
            StringAssert.Contains(result.Message, "first name");
            VolunteerProfileModel profile = (await context.Service.GetProfileAsync(context.UserId))!;
            Assert.AreEqual(string.Empty, profile.PersonalDetails.LastName);
        }

        // Test that an emergency contact name without a phone number is rejected.
        [TestMethod]
        public async Task UpdatePersonalDetailsAsync_EmergencyNameWithoutPhone_ReturnsFailure()
        {
            await using ProfileTestHarness context = await ProfileTestHarness.CreateAsync();
            PersonalDetailsInput input = ValidDetails();
            input.EmergencyContactPhone = null;

            ProfileResult result = await context.Service.UpdatePersonalDetailsAsync(context.UserId, input);

            Assert.IsFalse(result.Succeeded);
            StringAssert.Contains(result.Message, "emergency contact");
        }

        // Test that an invalid phone number is rejected.
        [TestMethod]
        public async Task UpdatePersonalDetailsAsync_InvalidPhone_ReturnsFailure()
        {
            await using ProfileTestHarness context = await ProfileTestHarness.CreateAsync();
            PersonalDetailsInput input = ValidDetails();
            input.PhoneNumber = "not a number";

            ProfileResult result = await context.Service.UpdatePersonalDetailsAsync(context.UserId, input);

            Assert.IsFalse(result.Succeeded);
        }

        // Test that saving skills cleans them up, removes duplicates and keeps the volunteer's order.
        [TestMethod]
        public async Task UpdateSkillsAsync_WithMessyList_SavesCleanList()
        {
            await using ProfileTestHarness context = await ProfileTestHarness.CreateAsync();

            ProfileResult result = await context.Service.UpdateSkillsAsync(
                context.UserId,
                [" First   aid ", "Driving", "first aid"]);

            Assert.IsTrue(result.Succeeded, result.Message);
            VolunteerProfileModel profile = (await context.Service.GetProfileAsync(context.UserId))!;
            CollectionAssert.AreEqual(new[] { "First aid", "Driving" }, profile.Skills.ToArray());
            Assert.HasCount(0, profile.Interests);
        }

        // Test that saving a new skills list replaces the old one, including reordering and removing entries.
        [TestMethod]
        public async Task UpdateSkillsAsync_SecondSave_ReplacesPreviousList()
        {
            await using ProfileTestHarness context = await ProfileTestHarness.CreateAsync();
            await context.Service.UpdateSkillsAsync(context.UserId, ["Cooking", "Driving", "Teaching"]);

            ProfileResult result = await context.Service.UpdateSkillsAsync(context.UserId, ["Teaching", "Cooking"]);

            Assert.IsTrue(result.Succeeded, result.Message);
            VolunteerProfileModel profile = (await context.Service.GetProfileAsync(context.UserId))!;
            CollectionAssert.AreEqual(new[] { "Teaching", "Cooking" }, profile.Skills.ToArray());
        }

        // Test that skills and interests are kept separate.
        [TestMethod]
        public async Task UpdateInterestsAsync_DoesNotChangeSkills()
        {
            await using ProfileTestHarness context = await ProfileTestHarness.CreateAsync();
            await context.Service.UpdateSkillsAsync(context.UserId, ["Driving"]);

            await context.Service.UpdateInterestsAsync(context.UserId, ["Environment", "Animals"]);

            VolunteerProfileModel profile = (await context.Service.GetProfileAsync(context.UserId))!;
            CollectionAssert.AreEqual(new[] { "Driving" }, profile.Skills.ToArray());
            CollectionAssert.AreEqual(new[] { "Environment", "Animals" }, profile.Interests.ToArray());
        }

        // Test that a list with too many skills is rejected.
        [TestMethod]
        public async Task UpdateSkillsAsync_TooManySkills_ReturnsFailure()
        {
            await using ProfileTestHarness context = await ProfileTestHarness.CreateAsync();
            IEnumerable<string> tooMany = Enumerable.Range(1, ProfileTagRules.MaxTags + 1)
                .Select(number => $"Skill {number}");

            ProfileResult result = await context.Service.UpdateSkillsAsync(context.UserId, tooMany);

            Assert.IsFalse(result.Succeeded);
        }

        // Test that a valid future availability window is saved.
        [TestMethod]
        public async Task AddAvailabilityAsync_FutureWindow_AddsSlot()
        {
            await using ProfileTestHarness context = await ProfileTestHarness.CreateAsync();

            ProfileResult result = await context.Service.AddAvailabilityAsync(
                context.UserId,
                Window(FixedNow.AddDays(1).AddHours(-1), hours: 3, note: "Can drive"));

            Assert.IsTrue(result.Succeeded, result.Message);
            AvailabilitySlotModel slot = (await context.Service.GetProfileAsync(context.UserId))!.Availability.Single();
            Assert.AreEqual(FixedNow.AddDays(1).AddHours(-1), slot.StartsAt);
            Assert.AreEqual(FixedNow.AddDays(1).AddHours(2), slot.EndsAt);
            Assert.AreEqual("Can drive", slot.Note);
        }

        // Test that a window whose end is before its start is rejected.
        [TestMethod]
        public async Task AddAvailabilityAsync_EndBeforeStart_ReturnsFailure()
        {
            await using ProfileTestHarness context = await ProfileTestHarness.CreateAsync();
            AvailabilityInput input = new()
            {
                StartsAt = FixedNow.AddDays(1),
                EndsAt = FixedNow.AddDays(1).AddHours(-2)
            };

            ProfileResult result = await context.Service.AddAvailabilityAsync(context.UserId, input);

            Assert.IsFalse(result.Succeeded);
            StringAssert.Contains(result.Message, "after the start time");
        }

        // Test that a window that has already finished is rejected.
        [TestMethod]
        public async Task AddAvailabilityAsync_PastWindow_ReturnsFailure()
        {
            await using ProfileTestHarness context = await ProfileTestHarness.CreateAsync();

            ProfileResult result = await context.Service.AddAvailabilityAsync(
                context.UserId,
                Window(FixedNow.AddDays(-2), hours: 3));

            Assert.IsFalse(result.Succeeded);
            StringAssert.Contains(result.Message, "already passed");
        }

        // Test that a window overlapping an existing one is rejected.
        [TestMethod]
        public async Task AddAvailabilityAsync_OverlappingWindow_ReturnsFailure()
        {
            await using ProfileTestHarness context = await ProfileTestHarness.CreateAsync();
            DateTime start = FixedNow.AddDays(2);
            await context.Service.AddAvailabilityAsync(context.UserId, Window(start, hours: 4));

            ProfileResult result = await context.Service.AddAvailabilityAsync(
                context.UserId,
                Window(start.AddHours(3), hours: 2));

            Assert.IsFalse(result.Succeeded);
            StringAssert.Contains(result.Message, "overlaps");
        }

        // Test that a window starting exactly when another ends is allowed (they only touch).
        [TestMethod]
        public async Task AddAvailabilityAsync_BackToBackWindows_AreAllowed()
        {
            await using ProfileTestHarness context = await ProfileTestHarness.CreateAsync();
            DateTime start = FixedNow.AddDays(2);
            await context.Service.AddAvailabilityAsync(context.UserId, Window(start, hours: 2));

            ProfileResult result = await context.Service.AddAvailabilityAsync(
                context.UserId,
                Window(start.AddHours(2), hours: 2));

            Assert.IsTrue(result.Succeeded, result.Message);
        }

        // Test that editing a window saves the new times (and doesn't count as overlapping itself).
        [TestMethod]
        public async Task UpdateAvailabilityAsync_OwnSlot_ChangesTimes()
        {
            await using ProfileTestHarness context = await ProfileTestHarness.CreateAsync();
            DateTime start = FixedNow.AddDays(3);
            await context.Service.AddAvailabilityAsync(context.UserId, Window(start, hours: 2));
            Guid slotId = (await context.Service.GetProfileAsync(context.UserId))!.Availability.Single().Id;

            ProfileResult result = await context.Service.UpdateAvailabilityAsync(
                context.UserId,
                slotId,
                Window(start.AddHours(1), hours: 4));

            Assert.IsTrue(result.Succeeded, result.Message);
            AvailabilitySlotModel slot = (await context.Service.GetProfileAsync(context.UserId))!.Availability.Single();
            Assert.AreEqual(start.AddHours(1), slot.StartsAt);
            Assert.AreEqual(start.AddHours(5), slot.EndsAt);
        }

        // Test that a volunteer can't edit someone else's availability.
        [TestMethod]
        public async Task UpdateAvailabilityAsync_OtherUsersSlot_ReturnsFailure()
        {
            await using ProfileTestHarness context = await ProfileTestHarness.CreateAsync();
            string otherUserId = await context.CreateUserAsync("other@example.com");
            await context.Service.AddAvailabilityAsync(otherUserId, Window(FixedNow.AddDays(1), hours: 2));
            Guid otherSlotId = (await context.Service.GetProfileAsync(otherUserId))!.Availability.Single().Id;

            ProfileResult result = await context.Service.UpdateAvailabilityAsync(
                context.UserId,
                otherSlotId,
                Window(FixedNow.AddDays(4), hours: 2));

            Assert.IsFalse(result.Succeeded);
            AvailabilitySlotModel unchanged = (await context.Service.GetProfileAsync(otherUserId))!.Availability.Single();
            Assert.AreEqual(FixedNow.AddDays(1), unchanged.StartsAt);
        }

        // Test that deleting a window removes it.
        [TestMethod]
        public async Task DeleteAvailabilityAsync_OwnSlot_RemovesIt()
        {
            await using ProfileTestHarness context = await ProfileTestHarness.CreateAsync();
            await context.Service.AddAvailabilityAsync(context.UserId, Window(FixedNow.AddDays(1), hours: 2));
            Guid slotId = (await context.Service.GetProfileAsync(context.UserId))!.Availability.Single().Id;

            ProfileResult result = await context.Service.DeleteAvailabilityAsync(context.UserId, slotId);

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.HasCount(0, (await context.Service.GetProfileAsync(context.UserId))!.Availability);
        }

        // Test that a volunteer can't delete someone else's availability.
        [TestMethod]
        public async Task DeleteAvailabilityAsync_OtherUsersSlot_ReturnsFailureAndKeepsSlot()
        {
            await using ProfileTestHarness context = await ProfileTestHarness.CreateAsync();
            string otherUserId = await context.CreateUserAsync("other@example.com");
            await context.Service.AddAvailabilityAsync(otherUserId, Window(FixedNow.AddDays(1), hours: 2));
            Guid otherSlotId = (await context.Service.GetProfileAsync(otherUserId))!.Availability.Single().Id;

            ProfileResult result = await context.Service.DeleteAvailabilityAsync(context.UserId, otherSlotId);

            Assert.IsFalse(result.Succeeded);
            Assert.HasCount(1, (await context.Service.GetProfileAsync(otherUserId))!.Availability);
        }

        // Test that a volunteer's history comes from their requests in the domain services.
        [TestMethod]
        public async Task GetHistory_ForVolunteerWithRequest_ReturnsEntry()
        {
            await using ProfileTestHarness context = await ProfileTestHarness.CreateAsync();
            Guid volunteerId = Guid.Parse(context.UserId);
            DateTime start = DateTime.UtcNow.AddDays(3);
            VolunteerOpportunity opportunity = context.Opportunities.CreateOpportunity(
                "Beach Cleanup", "Clean the beach.", "Mission Bay", start, start.AddHours(2), "Teamwork", 5);
            context.Requests.SubmitRequest(volunteerId, opportunity.Id);

            IReadOnlyList<VolunteerHistoryEntry> history = context.Service.GetHistory(volunteerId);

            Assert.HasCount(1, history);
            Assert.AreEqual("Beach Cleanup", history[0].EventTitle);
        }

        private static PersonalDetailsInput ValidDetails() => new()
        {
            FirstName = "Aroha",
            LastName = "Ngata",
            PhoneNumber = "021 123 4567",
            City = "Mt Eden, Auckland",
            Bio = "I love helping at community events.",
            EmergencyContactName = "Mere Ngata",
            EmergencyContactPhone = "021 765 4321"
        };

        private static AvailabilityInput Window(DateTime start, int hours, string? note = null) => new()
        {
            StartsAt = start,
            EndsAt = start.AddHours(hours),
            Note = note
        };

        // A clock that always returns FixedNow, with UTC as the "local" time zone so local and UTC times match.
        private sealed class FixedTimeProvider : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() =>
                new(DateTime.SpecifyKind(FixedNow, DateTimeKind.Utc));

            public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        }

        // Builds an isolated in-memory database, Identity, the domain services and the profile
        // service for one test, plus one signed-up volunteer.
        private sealed class ProfileTestHarness : IAsyncDisposable
        {
            private readonly ServiceProvider provider;
            private readonly AsyncServiceScope scope;

            private ProfileTestHarness(ServiceProvider provider)
            {
                this.provider = provider;
                scope = provider.CreateAsyncScope();
            }

            public string UserId { get; private set; } = string.Empty;

            public VolunteerProfileService Service => scope.ServiceProvider.GetRequiredService<VolunteerProfileService>();

            public VolunteerRequestService Requests => scope.ServiceProvider.GetRequiredService<VolunteerRequestService>();

            public VolunteerOpportunityService Opportunities => scope.ServiceProvider.GetRequiredService<VolunteerOpportunityService>();

            public static async Task<ProfileTestHarness> CreateAsync()
            {
                ServiceCollection services = new();
                string databaseName = Guid.NewGuid().ToString();

                services.AddLogging();
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(databaseName));
                services.AddIdentityCore<ApplicationUser>()
                    .AddEntityFrameworkStores<ApplicationDbContext>();
                services.AddSingleton<VolunteerRequestService>();
                services.AddSingleton<VolunteerOpportunityService>();
                services.AddSingleton<VolunteerHistoryService>();
                services.AddSingleton<TimeProvider>(new FixedTimeProvider());
                services.AddScoped<VolunteerProfileService>();

                ProfileTestHarness context = new(services.BuildServiceProvider());
                context.UserId = await context.CreateUserAsync("volunteer@example.com");
                return context;
            }

            // Creates another account and returns its id.
            public async Task<string> CreateUserAsync(string email)
            {
                UserManager<ApplicationUser> userManager =
                    scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                ApplicationUser user = new() { UserName = email, Email = email };

                IdentityResult result = await userManager.CreateAsync(user);
                Assert.IsTrue(result.Succeeded);
                return user.Id;
            }

            public async ValueTask DisposeAsync()
            {
                await scope.DisposeAsync();
                await provider.DisposeAsync();
            }
        }
    }
}
