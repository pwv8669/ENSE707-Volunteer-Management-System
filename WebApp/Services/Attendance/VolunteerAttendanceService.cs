using Microsoft.EntityFrameworkCore;
using WebApp.Data;

namespace WebApp.Services.Attendance
{
    // One persisted shift displayed on the coordinator attendance page.
    public sealed record AttendanceShiftModel(
        Guid ShiftId,
        string VolunteerId,
        string Title,
        string Location,
        DateTime StartsAt,
        DateTime EndsAt,
        VolunteerAttendanceStatus? AttendanceStatus,
        decimal HoursCompleted,
        string Notes,
        DateTime? RecordedAt);

    // One completed attendance result displayed in a volunteer's history.
    public sealed record VolunteerParticipationEntryModel(
        Guid ShiftId,
        string Title,
        string Location,
        DateTime StartsAt,
        DateTime EndsAt,
        VolunteerAttendanceStatus Status,
        decimal HoursCompleted,
        string Notes,
        DateTime RecordedAt);

    public sealed record VolunteerParticipationSummaryModel(
        int RecordedShifts,
        int AttendedShifts,
        int AbsentShifts,
        decimal TotalHours);

    // Persists Feature 6 attendance and completed hours in Supabase through
    // ApplicationDbContext. A fresh context is used for each operation so the
    // singleton service is safe to use from multiple Blazor circuits.
    public class VolunteerAttendanceService(
        IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        public const int MaxNotesLength = 500;

        // Volunteer pages subscribe to receive attendance corrections without
        // requiring the browser to be refreshed manually.
        public event Action<string>? AttendanceChanged;

        // Returns every active persisted shift, including rows that have not
        // had attendance recorded yet.
        public async Task<IReadOnlyList<AttendanceShiftModel>>
            GetShiftsForAttendanceAsync()
        {
            await using ApplicationDbContext db =
                await contextFactory.CreateDbContextAsync();

            return await (
                from shift in db.VolunteerShifts.AsNoTracking()
                where shift.Status == VolunteerShiftStatus.Assigned
                join attendance in db.VolunteerAttendanceRecords.AsNoTracking()
                    on shift.Id equals attendance.ShiftId into attendanceRows
                from attendance in attendanceRows.DefaultIfEmpty()
                orderby shift.StartsAt descending, shift.Title
                select new AttendanceShiftModel(
                    shift.Id,
                    shift.VolunteerId,
                    shift.Title,
                    shift.Location,
                    shift.StartsAt,
                    shift.EndsAt,
                    attendance == null
                        ? null
                        : attendance.Status,
                    attendance == null
                        ? 0m
                        : attendance.HoursCompleted,
                    attendance == null
                        ? string.Empty
                        : attendance.Notes,
                    attendance == null
                        ? null
                        : attendance.RecordedAt))
                .ToListAsync();
        }

        // Creates or corrects attendance for one assigned shift. Hours must be
        // positive for attendance, zero for absence, and no longer than the
        // volunteer's assigned shift.
        public async Task RecordAttendanceAsync(
            Guid shiftId,
            string recordedByUserId,
            VolunteerAttendanceStatus status,
            decimal hoursCompleted,
            string? notes)
        {
            if (shiftId == Guid.Empty)
            {
                throw new ArgumentException(
                    "A shift id is required.",
                    nameof(shiftId));
            }

            ValidateUserId(recordedByUserId, nameof(recordedByUserId));

            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    "A valid attendance status is required.");
            }

            string cleanedNotes = (notes ?? string.Empty).Trim();
            if (cleanedNotes.Length > MaxNotesLength)
            {
                throw new ArgumentException(
                    $"Notes cannot exceed {MaxNotesLength} characters.",
                    nameof(notes));
            }

            await using ApplicationDbContext db =
                await contextFactory.CreateDbContextAsync();
            VolunteerShiftRecord shift =
                await db.VolunteerShifts.FindAsync(shiftId)
                ?? throw new KeyNotFoundException(
                    "Volunteer shift was not found.");

            if (shift.Status != VolunteerShiftStatus.Assigned)
            {
                throw new InvalidOperationException(
                    "Attendance cannot be recorded for a cancelled shift.");
            }

            ValidateHours(shift, status, hoursCompleted);

            VolunteerAttendanceRecord? attendance =
                await db.VolunteerAttendanceRecords.FindAsync(shiftId);
            if (attendance is null)
            {
                attendance = new VolunteerAttendanceRecord
                {
                    ShiftId = shift.Id,
                    VolunteerId = shift.VolunteerId
                };
                db.VolunteerAttendanceRecords.Add(attendance);
            }

            attendance.Status = status;
            attendance.HoursCompleted = hoursCompleted;
            attendance.Notes = cleanedNotes;
            attendance.RecordedByUserId = recordedByUserId;
            attendance.RecordedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            AttendanceChanged?.Invoke(shift.VolunteerId);
        }

        // Returns recorded attendance in event order for the volunteer's
        // participation history. Unrecorded assignments are not completed
        // participation and are therefore omitted.
        public async Task<IReadOnlyList<VolunteerParticipationEntryModel>>
            GetVolunteerParticipationAsync(string volunteerId)
        {
            ValidateUserId(volunteerId, nameof(volunteerId));
            await using ApplicationDbContext db =
                await contextFactory.CreateDbContextAsync();

            return await (
                from attendance in db.VolunteerAttendanceRecords.AsNoTracking()
                join shift in db.VolunteerShifts.AsNoTracking()
                    on attendance.ShiftId equals shift.Id
                where attendance.VolunteerId == volunteerId
                orderby shift.StartsAt descending, shift.Title
                select new VolunteerParticipationEntryModel(
                    shift.Id,
                    shift.Title,
                    shift.Location,
                    shift.StartsAt,
                    shift.EndsAt,
                    attendance.Status,
                    attendance.HoursCompleted,
                    attendance.Notes,
                    attendance.RecordedAt))
                .ToListAsync();
        }

        public async Task<VolunteerParticipationSummaryModel>
            GetVolunteerSummaryAsync(string volunteerId)
        {
            IReadOnlyList<VolunteerParticipationEntryModel> history =
                await GetVolunteerParticipationAsync(volunteerId);

            return new VolunteerParticipationSummaryModel(
                history.Count,
                history.Count(item =>
                    item.Status == VolunteerAttendanceStatus.Attended),
                history.Count(item =>
                    item.Status == VolunteerAttendanceStatus.Absent),
                history.Sum(item => item.HoursCompleted));
        }

        private static void ValidateHours(
            VolunteerShiftRecord shift,
            VolunteerAttendanceStatus status,
            decimal hoursCompleted)
        {
            if (status == VolunteerAttendanceStatus.Absent)
            {
                if (hoursCompleted != 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(hoursCompleted),
                        "An absent volunteer must have zero completed hours.");
                }

                return;
            }

            if (hoursCompleted <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hoursCompleted),
                    "Completed hours must be greater than zero for attendance.");
            }

            decimal scheduledHours =
                (decimal)(shift.EndsAt - shift.StartsAt).TotalHours;
            if (hoursCompleted > scheduledHours)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hoursCompleted),
                    "Completed hours cannot exceed the assigned shift length.");
            }
        }

        private static void ValidateUserId(
            string userId,
            string parameterName)
        {
            if (!Guid.TryParse(userId, out _))
            {
                throw new ArgumentException(
                    "A valid user id is required.",
                    parameterName);
            }
        }
    }
}
