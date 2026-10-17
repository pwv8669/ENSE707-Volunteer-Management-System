using Microsoft.EntityFrameworkCore;
using Volunteer_Management_System;
using WebApp.Data;

namespace WebApp.Services.Activity
{
    // One saved shift together with its attendance result (if any), as read from the database.
    public sealed record ShiftActivity(
        Guid ShiftId,
        Guid VolunteerId,
        Guid OpportunityId,
        Guid ApplicationId,
        VolunteerShiftStatus ShiftStatus,
        DateTime AssignedAt,
        VolunteerAttendanceStatus? Attendance,
        decimal HoursCompleted);

    // Keeps the "requests" that the Reporting and Dashboard page and the profile history read
    // in step with what really happened in the app, instead of leaving them empty or filled with demo data.
    //
    // A request is worked out from a volunteer's application and their saved shifts:
    //   - application Pending                          -> request Pending (waiting for a coordinator)
    //   - application Rejected                         -> request Declined
    //   - application Approved                         -> request Fulfilled, with the hours recorded as attended
    //   - application Approved but every shift was cancelled -> request Declined
    // Shifts and attendance are saved in the database, but applications live in memory, so after an app restart
    // a saved shift whose application has gone still counts as a Fulfilled (or Declined, if cancelled) request.
    public sealed class VolunteerActivitySyncService(
        VolunteerApplicationService applicationService,
        VolunteerRequestService requestService,
        IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        // Only one refresh runs at a time, so an older result can never overwrite a newer one.
        private readonly SemaphoreSlim refreshGate = new(1, 1);

        // Reads the applications and saved shifts, and replaces the stored requests with the result.
        // Call this just before showing the dashboard or a volunteer's history.
        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            await refreshGate.WaitAsync(cancellationToken);
            try
            {
                List<ShiftActivity> shifts = await LoadShiftsAsync(cancellationToken);
                List<VolunteerApplication> applications = applicationService.GetAllApplications().ToList();

                requestService.ReplaceAll(BuildRequests(applications, shifts));
            }
            finally
            {
                refreshGate.Release();
            }
        }

        // Works out one request per application, plus one per group of saved shifts whose application is no longer in memory.
        public static IReadOnlyList<VolunteerRequest> BuildRequests(
            IEnumerable<VolunteerApplication> applications,
            IEnumerable<ShiftActivity> shifts)
        {
            ArgumentNullException.ThrowIfNull(applications);
            ArgumentNullException.ThrowIfNull(shifts);

            // Shifts without all three ids can't be matched to anything, so they are ignored.
            List<ShiftActivity> usableShifts = shifts
                .Where(shift => shift.ApplicationId != Guid.Empty
                    && shift.VolunteerId != Guid.Empty
                    && shift.OpportunityId != Guid.Empty)
                .ToList();
            ILookup<Guid, ShiftActivity> shiftsByApplication =
                usableShifts.ToLookup(shift => shift.ApplicationId);

            List<VolunteerRequest> requests = new();
            HashSet<Guid> applicationIds = new();

            foreach (VolunteerApplication application in applications)
            {
                applicationIds.Add(application.Id);
                List<ShiftActivity> applicationShifts = shiftsByApplication[application.Id].ToList();
                requests.Add(FromApplication(application, applicationShifts));
            }

            // Saved shifts whose application has gone (for example after the app restarted).
            foreach (IGrouping<Guid, ShiftActivity> group in usableShifts
                .Where(shift => !applicationIds.Contains(shift.ApplicationId))
                .GroupBy(shift => shift.ApplicationId))
            {
                requests.Add(FromShiftsOnly(group.Key, group.ToList()));
            }

            return requests.AsReadOnly();
        }

        // Turns one application (and the shifts saved for it) into a request.
        private static VolunteerRequest FromApplication(
            VolunteerApplication application,
            List<ShiftActivity> applicationShifts)
        {
            DateTime respondedAt = application.ReviewedAt ?? application.AppliedAt;

            switch (application.Status)
            {
                case VolunteerApplicationStatus.Pending:
                    return VolunteerRequest.Restore(
                        application.Id, application.VolunteerId, application.OpportunityId,
                        VolunteerRequestStatus.Pending, application.AppliedAt, null, 0);

                case VolunteerApplicationStatus.Rejected:
                    return VolunteerRequest.Restore(
                        application.Id, application.VolunteerId, application.OpportunityId,
                        VolunteerRequestStatus.Declined, application.AppliedAt, respondedAt, 0);

                default:
                    // Approved. If shifts were saved and all of them have been cancelled, the volunteer is no longer taking part.
                    bool cancelled = applicationShifts.Count > 0
                        && applicationShifts.All(shift => shift.ShiftStatus == VolunteerShiftStatus.Cancelled);

                    return cancelled
                        ? VolunteerRequest.Restore(
                            application.Id, application.VolunteerId, application.OpportunityId,
                            VolunteerRequestStatus.Declined, application.AppliedAt, respondedAt, 0)
                        : VolunteerRequest.Restore(
                            application.Id, application.VolunteerId, application.OpportunityId,
                            VolunteerRequestStatus.Fulfilled, application.AppliedAt, respondedAt,
                            AttendedHours(applicationShifts));
            }
        }

        // Turns shifts that have no application in memory into a request.
        private static VolunteerRequest FromShiftsOnly(Guid applicationId, List<ShiftActivity> applicationShifts)
        {
            ShiftActivity first = applicationShifts[0];
            DateTime assignedAt = applicationShifts.Min(shift => shift.AssignedAt);
            bool cancelled = applicationShifts.All(shift => shift.ShiftStatus == VolunteerShiftStatus.Cancelled);

            return VolunteerRequest.Restore(
                applicationId,
                first.VolunteerId,
                first.OpportunityId,
                cancelled ? VolunteerRequestStatus.Declined : VolunteerRequestStatus.Fulfilled,
                assignedAt,
                assignedAt,
                cancelled ? 0 : AttendedHours(applicationShifts));
        }

        // Adds up the hours the volunteer actually attended on shifts that are still active. Absences and cancelled shifts count as zero.
        private static double AttendedHours(IEnumerable<ShiftActivity> applicationShifts) =>
            (double)applicationShifts
                .Where(shift => shift.ShiftStatus == VolunteerShiftStatus.Assigned
                    && shift.Attendance == VolunteerAttendanceStatus.Attended)
                .Sum(shift => shift.HoursCompleted);

        // Reads every saved shift with its attendance record from the database.
        private async Task<List<ShiftActivity>> LoadShiftsAsync(CancellationToken cancellationToken)
        {
            await using ApplicationDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);

            var rows = await (
                from shift in db.VolunteerShifts.AsNoTracking()
                join attendance in db.VolunteerAttendanceRecords.AsNoTracking()
                    on shift.Id equals attendance.ShiftId into attendanceRows
                from attendance in attendanceRows.DefaultIfEmpty()
                select new
                {
                    shift.Id,
                    shift.VolunteerId,
                    shift.OpportunityId,
                    shift.ApplicationId,
                    shift.Status,
                    shift.AssignedAt,
                    Attendance = attendance == null ? (VolunteerAttendanceStatus?)null : attendance.Status,
                    Hours = attendance == null ? 0m : attendance.HoursCompleted
                })
                .ToListAsync(cancellationToken);

            List<ShiftActivity> shifts = new();
            foreach (var row in rows)
            {
                // Identity ids are Guids stored as text; anything else can't be matched to a volunteer.
                if (!Guid.TryParse(row.VolunteerId, out Guid volunteerId))
                {
                    continue;
                }

                shifts.Add(new ShiftActivity(
                    row.Id, volunteerId, row.OpportunityId, row.ApplicationId,
                    row.Status, row.AssignedAt, row.Attendance, row.Hours));
            }

            return shifts;
        }
    }
}
