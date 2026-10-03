using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Volunteer_Management_System;
using WebApp.Data;

namespace WebApp.Services.Profile
{
    // Reads and updates a volunteer's profile: personal details, skills, interests,
    // availability and volunteering history. Every method takes the signed-in user's id
    // and only ever touches that user's data, so one volunteer can't change another's profile.
    // All input is validated here again, even though the forms validate it first.
    public sealed class VolunteerProfileService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        VolunteerHistoryService historyService,
        TimeProvider clock)
    {
        // Most availability windows one volunteer can have saved.
        public const int MaxAvailabilitySlots = 50;

        private const string AccountNotFoundMessage =
            "We couldn't find your account. Please sign in again.";

        // Loads the whole profile for the page, or null if the account doesn't exist.
        public async Task<VolunteerProfileModel?> GetProfileAsync(string userId)
        {
            ApplicationUser? user = await userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return null;
            }

            List<AvailabilitySlotModel> availability = await db.VolunteerAvailabilitySlots
                .AsNoTracking()
                .Where(slot => slot.UserId == userId)
                .OrderBy(slot => slot.StartsAt)
                .Select(slot => new AvailabilitySlotModel(slot.Id, slot.StartsAt, slot.EndsAt, slot.Note))
                .ToListAsync();

            PersonalDetailsInput details = new()
            {
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                City = user.City,
                Bio = user.Bio,
                EmergencyContactName = user.EmergencyContactName,
                EmergencyContactPhone = user.EmergencyContactPhone
            };

            List<VolunteerProfileTag> tags = await db.VolunteerProfileTags
                .AsNoTracking()
                .Where(tag => tag.UserId == userId)
                .OrderBy(tag => tag.SortOrder)
                .ToListAsync();

            List<string> skills = TagValues(tags, ProfileTagKind.Skill);
            List<string> interests = TagValues(tags, ProfileTagKind.Interest);

            return new VolunteerProfileModel
            {
                UserId = user.Id,
                VolunteerId = ToVolunteerId(user.Id),
                Email = user.Email ?? user.UserName ?? string.Empty,
                PersonalDetails = details,
                Skills = skills,
                Interests = interests,
                Availability = availability,
                CompletionPercent = CalculateCompletion(details, skills, interests, availability, Now)
            };
        }

        // Saves the personal details section.
        public async Task<ProfileResult> UpdatePersonalDetailsAsync(string userId, PersonalDetailsInput input)
        {
            ArgumentNullException.ThrowIfNull(input);

            string? validationError = FirstValidationError(input);
            if (validationError is not null)
            {
                return ProfileResult.Failure(validationError);
            }

            ApplicationUser? user = await userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return ProfileResult.Failure(AccountNotFoundMessage);
            }

            // A new phone number hasn't been confirmed yet.
            if (!string.Equals(user.PhoneNumber, input.PhoneNumber, StringComparison.Ordinal))
            {
                user.PhoneNumber = input.PhoneNumber;
                user.PhoneNumberConfirmed = false;
            }

            user.FirstName = input.FirstName.Trim();
            user.LastName = input.LastName.Trim();
            user.City = input.City;
            user.Bio = input.Bio;
            user.EmergencyContactName = input.EmergencyContactName;
            user.EmergencyContactPhone = input.EmergencyContactPhone;

            // UpdateAsync (unlike SetPhoneNumberAsync) leaves the security stamp alone,
            // so saving the profile doesn't sign the volunteer out.
            IdentityResult result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                // Throw away the unsaved changes so the page keeps showing what's really stored.
                await db.Entry(user).ReloadAsync();
                return ProfileResult.Failure(Describe(result));
            }

            return ProfileResult.Success("Your personal details have been saved.");
        }

        // Replaces the volunteer's skills list.
        public Task<ProfileResult> UpdateSkillsAsync(string userId, IEnumerable<string> skills) =>
            UpdateTagsAsync(userId, skills, ProfileTagKind.Skill, "skills");

        // Replaces the volunteer's interests list.
        public Task<ProfileResult> UpdateInterestsAsync(string userId, IEnumerable<string> interests) =>
            UpdateTagsAsync(userId, interests, ProfileTagKind.Interest, "interests");

        // Adds a new availability window.
        public async Task<ProfileResult> AddAvailabilityAsync(string userId, AvailabilityInput input)
        {
            ArgumentNullException.ThrowIfNull(input);

            if (!TryReadWindow(input, out DateTime start, out DateTime end, out string? error))
            {
                return ProfileResult.Failure(error!);
            }

            if (!await db.Users.AnyAsync(user => user.Id == userId))
            {
                return ProfileResult.Failure(AccountNotFoundMessage);
            }

            List<VolunteerAvailabilitySlot> existing = await LoadSlotsAsync(userId);

            if (existing.Count >= MaxAvailabilitySlots)
            {
                return ProfileResult.Failure(
                    $"You can save up to {MaxAvailabilitySlots} availability times. Delete some old ones first.");
            }

            string? overlapError = FindOverlap(existing, start, end, ignoreSlotId: null);
            if (overlapError is not null)
            {
                return ProfileResult.Failure(overlapError);
            }

            db.VolunteerAvailabilitySlots.Add(new VolunteerAvailabilitySlot
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                StartsAt = start,
                EndsAt = end,
                Note = input.Note,
                CreatedAt = clock.GetUtcNow().UtcDateTime
            });

            await db.SaveChangesAsync();
            return ProfileResult.Success("Availability added.");
        }

        // Changes an existing availability window.
        public async Task<ProfileResult> UpdateAvailabilityAsync(string userId, Guid slotId, AvailabilityInput input)
        {
            ArgumentNullException.ThrowIfNull(input);

            if (!TryReadWindow(input, out DateTime start, out DateTime end, out string? error))
            {
                return ProfileResult.Failure(error!);
            }

            List<VolunteerAvailabilitySlot> existing = await LoadSlotsAsync(userId);

            // Only slots that belong to this user are loaded, so another volunteer's slot is never found here.
            VolunteerAvailabilitySlot? slot = existing.FirstOrDefault(item => item.Id == slotId);
            if (slot is null)
            {
                return ProfileResult.Failure("That availability time no longer exists. Refresh the page and try again.");
            }

            string? overlapError = FindOverlap(existing, start, end, ignoreSlotId: slotId);
            if (overlapError is not null)
            {
                return ProfileResult.Failure(overlapError);
            }

            slot.StartsAt = start;
            slot.EndsAt = end;
            slot.Note = input.Note;

            await db.SaveChangesAsync();
            return ProfileResult.Success("Availability updated.");
        }

        // Deletes an availability window.
        public async Task<ProfileResult> DeleteAvailabilityAsync(string userId, Guid slotId)
        {
            VolunteerAvailabilitySlot? slot = await db.VolunteerAvailabilitySlots
                .FirstOrDefaultAsync(item => item.Id == slotId && item.UserId == userId);

            if (slot is null)
            {
                return ProfileResult.Failure("That availability time no longer exists. Refresh the page and try again.");
            }

            db.VolunteerAvailabilitySlots.Remove(slot);
            await db.SaveChangesAsync();
            return ProfileResult.Success("Availability removed.");
        }

        // The volunteer's history and totals, from the domain's VolunteerHistoryService.
        public IReadOnlyList<VolunteerHistoryEntry> GetHistory(Guid volunteerId) =>
            volunteerId == Guid.Empty ? [] : historyService.GetHistory(volunteerId);

        public VolunteerHistorySummary GetHistorySummary(Guid volunteerId) =>
            volunteerId == Guid.Empty ? new VolunteerHistorySummary() : historyService.GetSummary(volunteerId);

        // Identity ids are Guids stored as strings; the domain services use Guid volunteer ids.
        public static Guid ToVolunteerId(string userId) =>
            Guid.TryParse(userId, out Guid volunteerId) ? volunteerId : Guid.Empty;

        // The current local time, used to decide what counts as "in the past".
        private DateTime Now => clock.GetLocalNow().DateTime;

        // Replaces one list (skills or interests): the old rows are removed and the new list is saved in order.
        private async Task<ProfileResult> UpdateTagsAsync(
            string userId,
            IEnumerable<string> tags,
            ProfileTagKind kind,
            string label)
        {
            ArgumentNullException.ThrowIfNull(tags);

            // Uses the shared domain rules, so the same limits apply everywhere.
            if (!ProfileTagRules.TryNormalizeList(tags, out IReadOnlyList<string> cleaned, out string? error))
            {
                return ProfileResult.Failure(error!);
            }

            if (!await db.Users.AnyAsync(user => user.Id == userId))
            {
                return ProfileResult.Failure(AccountNotFoundMessage);
            }

            List<VolunteerProfileTag> existing = await db.VolunteerProfileTags
                .Where(tag => tag.UserId == userId && tag.Kind == kind)
                .ToListAsync();

            // Only rows that actually changed are touched: removed entries are deleted, kept ones
            // get their new position, and new ones are added. This never clashes with the unique index.
            HashSet<string> wanted = new(cleaned, StringComparer.Ordinal);
            db.VolunteerProfileTags.RemoveRange(existing.Where(tag => !wanted.Contains(tag.Value)));

            Dictionary<string, VolunteerProfileTag> current =
                existing.ToDictionary(tag => tag.Value, StringComparer.Ordinal);

            for (int index = 0; index < cleaned.Count; index++)
            {
                if (current.TryGetValue(cleaned[index], out VolunteerProfileTag? kept))
                {
                    kept.SortOrder = index;
                    continue;
                }

                db.VolunteerProfileTags.Add(new VolunteerProfileTag
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Kind = kind,
                    Value = cleaned[index],
                    SortOrder = index
                });
            }

            await db.SaveChangesAsync();
            return ProfileResult.Success($"Your {label} have been saved.");
        }

        private static List<string> TagValues(IEnumerable<VolunteerProfileTag> tags, ProfileTagKind kind) =>
            tags.Where(tag => tag.Kind == kind).Select(tag => tag.Value).ToList();

        private Task<List<VolunteerAvailabilitySlot>> LoadSlotsAsync(string userId) =>
            db.VolunteerAvailabilitySlots
                .Where(slot => slot.UserId == userId)
                .OrderBy(slot => slot.StartsAt)
                .ToListAsync();

        // Validates an availability form and reads its two times. Times are stored as local
        // wall-clock times with no time zone, which is what Postgres "timestamp without time zone" expects.
        private bool TryReadWindow(AvailabilityInput input, out DateTime start, out DateTime end, out string? error)
        {
            start = default;
            end = default;
            error = FirstValidationError(input);

            if (error is not null)
            {
                return false;
            }

            start = DateTime.SpecifyKind(input.StartsAt!.Value, DateTimeKind.Unspecified);
            end = DateTime.SpecifyKind(input.EndsAt!.Value, DateTimeKind.Unspecified);

            if (end <= Now)
            {
                error = "That time has already passed. Choose a time in the future.";
                return false;
            }

            return true;
        }

        // Two windows overlap if each one starts before the other ends. Returns an error message, or null.
        private static string? FindOverlap(
            IEnumerable<VolunteerAvailabilitySlot> slots,
            DateTime start,
            DateTime end,
            Guid? ignoreSlotId)
        {
            VolunteerAvailabilitySlot? clash = slots.FirstOrDefault(slot =>
                slot.Id != ignoreSlotId &&
                slot.StartsAt < end &&
                start < slot.EndsAt);

            return clash is null
                ? null
                : $"This overlaps your availability on {ProfileFormat.Range(clash.StartsAt, clash.EndsAt)}. Edit that time instead, or pick a different one.";
        }

        // Runs the data annotation and IValidatableObject rules and returns the first error, or null.
        private static string? FirstValidationError(object model)
        {
            List<ValidationResult> results = new();
            bool valid = Validator.TryValidateObject(
                model,
                new ValidationContext(model),
                results,
                validateAllProperties: true);

            return valid ? null : results[0].ErrorMessage ?? "Please check the form and try again.";
        }

        // Ten items, worth 10% each: the seven personal fields, at least one skill,
        // at least one interest, and at least one upcoming availability window.
        private static int CalculateCompletion(
            PersonalDetailsInput details,
            IReadOnlyList<string> skills,
            IReadOnlyList<string> interests,
            IReadOnlyList<AvailabilitySlotModel> availability,
            DateTime now)
        {
            bool[] items =
            [
                details.FirstName.Length > 0,
                details.LastName.Length > 0,
                details.PhoneNumber is not null,
                details.City is not null,
                details.Bio is not null,
                details.EmergencyContactName is not null,
                details.EmergencyContactPhone is not null,
                skills.Count > 0,
                interests.Count > 0,
                availability.Any(slot => !slot.IsPast(now))
            ];

            return items.Count(done => done) * 100 / items.Length;
        }

        private static string Describe(IdentityResult result) =>
            "Your changes couldn't be saved: " +
            string.Join(" ", result.Errors.Select(error => error.Description));
    }
}
