using Microsoft.EntityFrameworkCore;
using Volunteer_Management_System;
using WebApp.Data;

namespace WebApp.Services.Scheduling
{
    // Provides the data displayed on the volunteer schedule page without
    // exposing Entity Framework entities directly to the UI.
    public sealed record VolunteerScheduledShiftModel(
        Guid AssignmentId,
        Guid OpportunityId,
        string Title,
        string Location,
        string RequiredSkills,
        DateTime StartsAt,
        DateTime EndsAt,
        DateTime AssignedAt);

    public sealed record VolunteerShiftNotificationModel(
        Guid Id,
        Guid ShiftId,
        VolunteerShiftNotificationType Type,
        string OpportunityTitle,
        DateTime ShiftStartsAt,
        DateTime CreatedAt,
        bool IsRead);

    // Persists Feature 5 schedules and notifications in the configured
    // ApplicationDbContext, which uses the team's Supabase Postgres database.
    public class VolunteerSchedulePersistenceService(
        IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        // Components subscribe to this event to refresh when their volunteer's
        // persisted schedule or notification state changes.
        public event Action<string>? ScheduleChanged;

        // Saves an assigned shift and its unread notification together.
        public async Task RecordAssignmentAsync(
            VolunteerAssignment assignment,
            VolunteerOpportunity opportunity)
        {
            ArgumentNullException.ThrowIfNull(assignment);
            ArgumentNullException.ThrowIfNull(opportunity);

            await using ApplicationDbContext db =
                await contextFactory.CreateDbContextAsync();
            string volunteerId = assignment.VolunteerId.ToString();

            VolunteerShiftRecord? shift =
                await db.VolunteerShifts.FindAsync(assignment.Id);
            if (shift is null)
            {
                shift = new VolunteerShiftRecord
                {
                    Id = assignment.Id,
                    VolunteerId = volunteerId,
                    OpportunityId = assignment.OpportunityId,
                    ApplicationId = assignment.ApplicationId,
                    AssignedByUserId = assignment.AssignedByUserId,
                    AssignedAt = EnsureUtc(assignment.AssignedAt)
                };
                db.VolunteerShifts.Add(shift);
            }

            CopyOpportunityDetails(shift, opportunity);
            shift.Status = VolunteerShiftStatus.Assigned;
            shift.CancelledAt = null;

            db.VolunteerShiftNotifications.Add(
                CreateNotification(
                    shift,
                    VolunteerShiftNotificationType.Assigned));

            await db.SaveChangesAsync();
            ScheduleChanged?.Invoke(volunteerId);
        }

        // Marks the persisted shift as cancelled and creates a cancellation
        // notification that remains visible after the active shift disappears.
        public async Task RecordCancellationAsync(
            VolunteerAssignment assignment,
            VolunteerOpportunity opportunity)
        {
            ArgumentNullException.ThrowIfNull(assignment);
            ArgumentNullException.ThrowIfNull(opportunity);

            await using ApplicationDbContext db =
                await contextFactory.CreateDbContextAsync();
            string volunteerId = assignment.VolunteerId.ToString();
            VolunteerShiftRecord? shift =
                await db.VolunteerShifts.FindAsync(assignment.Id);

            if (shift is null)
            {
                shift = new VolunteerShiftRecord
                {
                    Id = assignment.Id,
                    VolunteerId = volunteerId,
                    OpportunityId = assignment.OpportunityId,
                    ApplicationId = assignment.ApplicationId,
                    AssignedByUserId = assignment.AssignedByUserId,
                    AssignedAt = EnsureUtc(assignment.AssignedAt)
                };
                CopyOpportunityDetails(shift, opportunity);
                db.VolunteerShifts.Add(shift);
            }

            shift.Status = VolunteerShiftStatus.Cancelled;
            shift.CancelledAt = DateTime.UtcNow;
            db.VolunteerShiftNotifications.Add(
                CreateNotification(
                    shift,
                    VolunteerShiftNotificationType.Cancelled));

            await db.SaveChangesAsync();
            ScheduleChanged?.Invoke(volunteerId);
        }

        // Returns active shifts in chronological order, including shifts saved
        // by an earlier WebApp process.
        public async Task<IReadOnlyList<VolunteerScheduledShiftModel>>
            GetScheduleAsync(string volunteerId)
        {
            ValidateVolunteerId(volunteerId);
            await using ApplicationDbContext db =
                await contextFactory.CreateDbContextAsync();

            return await db.VolunteerShifts
                .AsNoTracking()
                .Where(shift =>
                    shift.VolunteerId == volunteerId &&
                    shift.Status == VolunteerShiftStatus.Assigned)
                .OrderBy(shift => shift.StartsAt)
                .ThenBy(shift => shift.Title)
                .Select(shift => new VolunteerScheduledShiftModel(
                    shift.Id,
                    shift.OpportunityId,
                    shift.Title,
                    shift.Location,
                    shift.RequiredSkills,
                    shift.StartsAt,
                    shift.EndsAt,
                    shift.AssignedAt))
                .ToListAsync();
        }

        public async Task<IReadOnlyList<VolunteerShiftNotificationModel>>
            GetNotificationsAsync(string volunteerId)
        {
            ValidateVolunteerId(volunteerId);
            await using ApplicationDbContext db =
                await contextFactory.CreateDbContextAsync();

            return await db.VolunteerShiftNotifications
                .AsNoTracking()
                .Where(notification =>
                    notification.VolunteerId == volunteerId)
                .OrderByDescending(notification =>
                    notification.CreatedAt)
                .Select(notification => new VolunteerShiftNotificationModel(
                    notification.Id,
                    notification.ShiftId,
                    notification.Type,
                    notification.OpportunityTitle,
                    notification.ShiftStartsAt,
                    notification.CreatedAt,
                    notification.IsRead))
                .ToListAsync();
        }

        public async Task<int> GetUnreadNotificationCountAsync(
            string volunteerId)
        {
            ValidateVolunteerId(volunteerId);
            await using ApplicationDbContext db =
                await contextFactory.CreateDbContextAsync();

            return await db.VolunteerShiftNotifications.CountAsync(
                notification =>
                    notification.VolunteerId == volunteerId &&
                    !notification.IsRead);
        }

        public async Task MarkAllNotificationsAsReadAsync(
            string volunteerId)
        {
            ValidateVolunteerId(volunteerId);
            await using ApplicationDbContext db =
                await contextFactory.CreateDbContextAsync();
            List<VolunteerShiftNotificationRecord> unread =
                await db.VolunteerShiftNotifications
                    .Where(notification =>
                        notification.VolunteerId == volunteerId &&
                        !notification.IsRead)
                    .ToListAsync();

            if (unread.Count == 0)
            {
                return;
            }

            foreach (VolunteerShiftNotificationRecord notification in unread)
            {
                notification.IsRead = true;
            }

            await db.SaveChangesAsync();
            ScheduleChanged?.Invoke(volunteerId);
        }

        // Checks persisted shifts as well as the current in-memory assignments,
        // preventing conflicts after the application has restarted.
        public async Task<bool> HasConflictAsync(
            string volunteerId,
            DateTime startsAt,
            DateTime endsAt)
        {
            ValidateVolunteerId(volunteerId);
            DateTime start = EnsureLocal(startsAt);
            DateTime end = EnsureLocal(endsAt);
            await using ApplicationDbContext db =
                await contextFactory.CreateDbContextAsync();

            return await db.VolunteerShifts.AnyAsync(shift =>
                shift.VolunteerId == volunteerId &&
                shift.Status == VolunteerShiftStatus.Assigned &&
                start < shift.EndsAt &&
                end > shift.StartsAt);
        }

        private static VolunteerShiftNotificationRecord CreateNotification(
            VolunteerShiftRecord shift,
            VolunteerShiftNotificationType type) =>
            new()
            {
                Id = Guid.NewGuid(),
                VolunteerId = shift.VolunteerId,
                ShiftId = shift.Id,
                Type = type,
                OpportunityTitle = shift.Title,
                ShiftStartsAt = shift.StartsAt,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

        private static void CopyOpportunityDetails(
            VolunteerShiftRecord shift,
            VolunteerOpportunity opportunity)
        {
            shift.Title = opportunity.Title;
            shift.Location = opportunity.Location;
            shift.RequiredSkills = opportunity.RequiredSkills;
            shift.StartsAt = EnsureLocal(opportunity.StartDateTime);
            shift.EndsAt = EnsureLocal(opportunity.EndDateTime);
        }

        // PostgreSQL distinguishes local timestamps from UTC instants.
        private static DateTime EnsureLocal(DateTime value) =>
            DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

        private static DateTime EnsureUtc(DateTime value) =>
            value.Kind == DateTimeKind.Utc
                ? value
                : value.ToUniversalTime();

        private static void ValidateVolunteerId(string volunteerId)
        {
            if (!Guid.TryParse(volunteerId, out _))
            {
                throw new ArgumentException(
                    "A valid volunteer id is required.",
                    nameof(volunteerId));
            }
        }
    }
}
