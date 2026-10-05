using Microsoft.EntityFrameworkCore;
using Volunteer_Management_System;
using WebApp.Data;

namespace WebApp.Services
{
    // -------------------------------------------------------------------------
    // AvailabilitySyncService.cs
    //
    // Purpose:
    // Connects the availability saved by the WebApp Volunteer Profile feature
    // with the VolunteerAvailabilityService used by Feature 4 assignment logic.
    //
    // The Profile feature stores availability in PostgreSQL, while the existing
    // Feature 4 domain service keeps availability in memory. Before a volunteer
    // is assigned, this service copies their latest saved Profile availability
    // into the domain service so the existing assignment availability checks
    // can still be used.
    //
    // This avoids creating a second availability page or replacing the
    // Volunteer Profile functionality created by another team member.
    // -------------------------------------------------------------------------

    public class AvailabilitySyncService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly VolunteerAvailabilityService _availabilityService;

        public AvailabilitySyncService(
            ApplicationDbContext dbContext,
            VolunteerAvailabilityService availabilityService)
        {
            _dbContext =
                dbContext
                ?? throw new ArgumentNullException(
                    nameof(dbContext));

            _availabilityService =
                availabilityService
                ?? throw new ArgumentNullException(
                    nameof(availabilityService));
        }

        // Synchronises one volunteer's Profile availability with the
        // in-memory VolunteerAvailabilityService used by Feature 4.
        //
        // Existing in-memory availability for the volunteer is removed first
        // so edited or deleted Profile availability does not remain stale.
        public async Task SyncVolunteerAvailabilityAsync(
            Guid volunteerId)
        {
            if (volunteerId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Volunteer id is required.",
                    nameof(volunteerId));
            }

            string identityUserId =
                volunteerId.ToString();

            ApplicationUser? identityUser =
                await _dbContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        user =>
                            user.Id == identityUserId);

            if (identityUser == null)
            {
                throw new InvalidOperationException(
                    "The volunteer account could not be found.");
            }

            // Creates the domain Volunteer using the same ID as the
            // ASP.NET Identity account.
            User volunteer =
                User.Create(
                    volunteerId,
                    identityUser.UserName
                        ?? identityUser.Email
                        ?? identityUserId,
                    identityUser.Email
                        ?? identityUser.UserName
                        ?? identityUserId,
                    Role.Volunteer);

            // Remove previous in-memory availability so the domain service
            // always receives the latest version saved in the Profile.
            IReadOnlyList<VolunteerAvailability>
                existingAvailability =
                    _availabilityService
                        .GetAvailabilityForVolunteer(
                            volunteerId);

            foreach (VolunteerAvailability availability
                in existingAvailability)
            {
                _availabilityService.RemoveAvailability(
                    volunteer,
                    availability.Id);
            }

            // Retrieve the volunteer's availability from the Profile
            // database table created by the existing Profile feature.
            List<VolunteerAvailabilitySlot>
                savedAvailability =
                    await _dbContext
                        .VolunteerAvailabilitySlots
                        .AsNoTracking()
                        .Where(slot =>
                            slot.UserId ==
                                identityUserId)
                        .OrderBy(slot =>
                            slot.StartsAt)
                        .ToListAsync();

            // Copy each Profile availability window into the existing
            // Feature 4 domain availability service.
            foreach (VolunteerAvailabilitySlot slot
                in savedAvailability)
            {
                _availabilityService.AddAvailability(
                    volunteer,
                    slot.StartsAt,
                    slot.EndsAt);
            }
        }
    }
}
