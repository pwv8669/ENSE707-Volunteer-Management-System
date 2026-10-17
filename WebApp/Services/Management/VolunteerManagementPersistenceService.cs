using Microsoft.EntityFrameworkCore;
using Volunteer_Management_System;
using WebApp.Data;
using WebApp.Services.Scheduling;

namespace WebApp.Services.Management
{
    // Adds the opportunity details needed by the volunteer application page.
    public sealed record VolunteerApplicationView(
        Guid Id,
        Guid OpportunityId,
        string Title,
        string Location,
        VolunteerApplicationStatus Status,
        DateTime AppliedAt);

    // Saves Features 2, 3 and 4 in Supabase through ApplicationDbContext.
    // Domain objects are still returned to the UI, preserving the existing
    // validation messages and page structure while making the data durable.
    public sealed class VolunteerManagementPersistenceService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        VolunteerAvailabilityService availabilityService,
        VolunteerSchedulePersistenceService schedulePersistenceService)
    {
        public async Task<IReadOnlyList<VolunteerOpportunity>> GetOpportunitiesAsync(
            bool includeUnpublished,
            CancellationToken cancellationToken = default)
        {
            await using ApplicationDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
            IQueryable<VolunteerOpportunityRecord> query = db.VolunteerOpportunities.AsNoTracking();
            if (!includeUnpublished)
            {
                query = query.Where(item => item.Status == OpportunityStatus.Published);
            }

            return (await query.OrderBy(item => item.StartDateTime).ToListAsync(cancellationToken))
                .Select(ToDomain).ToList().AsReadOnly();
        }

        public async Task<VolunteerOpportunity> CreateOpportunityAsync(
            string title, string description, string location, DateTime startsAt,
            DateTime endsAt, string requiredSkills, int volunteersNeeded,
            CancellationToken cancellationToken = default)
        {
            VolunteerOpportunity opportunity = VolunteerOpportunity.Create(
                title, description, location, startsAt, endsAt, requiredSkills, volunteersNeeded);
            await using ApplicationDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
            db.VolunteerOpportunities.Add(ToRecord(opportunity));
            await db.SaveChangesAsync(cancellationToken);
            return opportunity;
        }

        public async Task UpdateOpportunityAsync(Guid id, string title, string description,
            string location, DateTime startsAt, DateTime endsAt, string requiredSkills,
            int volunteersNeeded, CancellationToken cancellationToken = default)
        {
            await using ApplicationDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
            VolunteerOpportunityRecord record = await FindOpportunityAsync(db, id, cancellationToken);
            VolunteerOpportunity updated = ToDomain(record);
            updated.UpdateDetails(title, description, location, startsAt, endsAt, requiredSkills, volunteersNeeded);
            Copy(updated, record);
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task PublishOpportunityAsync(Guid id, CancellationToken cancellationToken = default) =>
            await ChangeOpportunityStatusAsync(id, item => item.Publish(), cancellationToken);

        public async Task ArchiveOpportunityAsync(Guid id, CancellationToken cancellationToken = default) =>
            await ChangeOpportunityStatusAsync(id, item => item.Archive(), cancellationToken);

        public async Task<bool> DeleteOpportunityAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await using ApplicationDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
            VolunteerOpportunityRecord? record = await db.VolunteerOpportunities.FindAsync([id], cancellationToken);
            if (record is null) return false;
            db.VolunteerOpportunities.Remove(record);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<VolunteerApplication> SubmitApplicationAsync(User volunteer, Guid opportunityId,
            CancellationToken cancellationToken = default)
        {
            if (volunteer.Role != Role.Volunteer)
                throw new UnauthorizedAccessException("Only volunteers can submit applications.");

            await using ApplicationDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
            VolunteerOpportunityRecord opportunity = await FindOpportunityAsync(db, opportunityId, cancellationToken);
            if (opportunity.Status != OpportunityStatus.Published)
                throw new InvalidOperationException("Applications can only be submitted for published opportunities.");
            string volunteerId = volunteer.Id.ToString();
            if (await db.VolunteerApplications.AnyAsync(item => item.VolunteerId == volunteerId && item.OpportunityId == opportunityId, cancellationToken))
                throw new InvalidOperationException("Volunteer has already applied for this opportunity.");

            VolunteerApplication application = VolunteerApplication.Create(volunteer.Id, opportunityId);
            db.VolunteerApplications.Add(ToRecord(application));
            await db.SaveChangesAsync(cancellationToken);
            return application;
        }

        public async Task<IReadOnlyList<VolunteerApplication>> GetApplicationsForVolunteerAsync(Guid volunteerId,
            CancellationToken cancellationToken = default) =>
            await GetApplicationsAsync(item => item.VolunteerId == volunteerId.ToString(), cancellationToken);

        public async Task<IReadOnlyList<VolunteerApplicationView>> GetApplicationViewsForVolunteerAsync(Guid volunteerId,
            CancellationToken cancellationToken = default)
        {
            string id = volunteerId.ToString();
            await using ApplicationDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
            return await (
                from application in db.VolunteerApplications.AsNoTracking()
                join opportunity in db.VolunteerOpportunities.AsNoTracking()
                    on application.OpportunityId equals opportunity.Id
                where application.VolunteerId == id
                orderby application.AppliedAt descending
                select new VolunteerApplicationView(
                    application.Id, application.OpportunityId, opportunity.Title,
                    opportunity.Location, application.Status, application.AppliedAt))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<VolunteerApplication>> GetPendingApplicationsForOpportunityAsync(Guid opportunityId,
            CancellationToken cancellationToken = default) =>
            await GetApplicationsAsync(item => item.OpportunityId == opportunityId && item.Status == VolunteerApplicationStatus.Pending, cancellationToken);

        public async Task<IReadOnlyList<VolunteerAssignment>> GetAssignmentsForOpportunityAsync(Guid opportunityId,
            CancellationToken cancellationToken = default)
        {
            await using ApplicationDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
            return (await db.VolunteerAssignments.AsNoTracking()
                .Where(item => item.OpportunityId == opportunityId && item.Status == AssignmentStatus.Assigned)
                .OrderBy(item => item.AssignedAt).ToListAsync(cancellationToken))
                .Select(ToDomain).ToList().AsReadOnly();
        }

        public async Task<int> GetRemainingCapacityAsync(Guid opportunityId, CancellationToken cancellationToken = default)
        {
            await using ApplicationDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
            VolunteerOpportunityRecord opportunity = await FindOpportunityAsync(db, opportunityId, cancellationToken);
            int assigned = await db.VolunteerAssignments.CountAsync(item => item.OpportunityId == opportunityId && item.Status == AssignmentStatus.Assigned, cancellationToken);
            return Math.Max(0, opportunity.VolunteersNeeded - assigned);
        }

        // Approves the application, records the assignment, shift and notification in one database save.
        public async Task ApproveAndAssignAsync(User reviewer, Guid applicationId, CancellationToken cancellationToken = default)
        {
            ValidateReviewer(reviewer);
            await using ApplicationDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
            VolunteerApplicationRecord application = await FindApplicationAsync(db, applicationId, cancellationToken);
            if (application.Status != VolunteerApplicationStatus.Pending)
                throw new InvalidOperationException("Only pending applications can be reviewed.");
            VolunteerOpportunityRecord opportunity = await FindOpportunityAsync(db, application.OpportunityId, cancellationToken);
            if (opportunity.Status != OpportunityStatus.Published)
                throw new InvalidOperationException("Volunteers can only be assigned to published opportunities.");
            int assigned = await db.VolunteerAssignments.CountAsync(item => item.OpportunityId == opportunity.Id && item.Status == AssignmentStatus.Assigned, cancellationToken);
            if (assigned >= opportunity.VolunteersNeeded)
                throw new InvalidOperationException("This opportunity has reached its volunteer capacity.");
            if (!Guid.TryParse(application.VolunteerId, out Guid volunteerId))
                throw new InvalidOperationException("The application has an invalid volunteer account.");
            if (!availabilityService.IsVolunteerAvailable(volunteerId, opportunity.StartDateTime, opportunity.EndDateTime))
                throw new InvalidOperationException("Volunteer is not available during this opportunity.");
            bool overlaps = await db.VolunteerAssignments
                .Where(item => item.VolunteerId == application.VolunteerId && item.Status == AssignmentStatus.Assigned)
                .Join(db.VolunteerOpportunities, assignment => assignment.OpportunityId, existing => existing.Id,
                    (assignment, existing) => new { existing.StartDateTime, existing.EndDateTime })
                .AnyAsync(item => opportunity.StartDateTime < item.EndDateTime && opportunity.EndDateTime > item.StartDateTime, cancellationToken);
            if (overlaps)
                throw new InvalidOperationException("Volunteer already has an overlapping assignment.");

            DateTime now = DateTime.UtcNow;
            Guid assignmentId = Guid.NewGuid();
            application.Status = VolunteerApplicationStatus.Approved;
            application.ReviewedAt = now;
            application.ReviewedByUserId = reviewer.Id.ToString();
            db.VolunteerAssignments.Add(new VolunteerAssignmentRecord { Id = assignmentId, VolunteerId = application.VolunteerId, OpportunityId = opportunity.Id, ApplicationId = application.Id, AssignedByUserId = reviewer.Id.ToString(), AssignedAt = now, Status = AssignmentStatus.Assigned });
            VolunteerShiftRecord shift = new() { Id = assignmentId, VolunteerId = application.VolunteerId, OpportunityId = opportunity.Id, ApplicationId = application.Id, AssignedByUserId = reviewer.Id, AssignedAt = now, Title = opportunity.Title, Location = opportunity.Location, RequiredSkills = opportunity.RequiredSkills, StartsAt = Local(opportunity.StartDateTime), EndsAt = Local(opportunity.EndDateTime), Status = VolunteerShiftStatus.Assigned };
            db.VolunteerShifts.Add(shift);
            db.VolunteerShiftNotifications.Add(Notification(shift, VolunteerShiftNotificationType.Assigned));
            await db.SaveChangesAsync(cancellationToken);
            schedulePersistenceService.NotifyScheduleChanged(application.VolunteerId);
        }

        public async Task RejectApplicationAsync(User reviewer, Guid applicationId, CancellationToken cancellationToken = default)
        {
            ValidateReviewer(reviewer);
            await using ApplicationDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
            VolunteerApplicationRecord application = await FindApplicationAsync(db, applicationId, cancellationToken);
            if (application.Status != VolunteerApplicationStatus.Pending) throw new InvalidOperationException("Only pending applications can be reviewed.");
            application.Status = VolunteerApplicationStatus.Rejected;
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewedByUserId = reviewer.Id.ToString();
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task CancelAssignmentAsync(User reviewer, Guid assignmentId, CancellationToken cancellationToken = default)
        {
            ValidateReviewer(reviewer);
            await using ApplicationDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
            VolunteerAssignmentRecord assignment = await db.VolunteerAssignments.FindAsync([assignmentId], cancellationToken)
                ?? throw new KeyNotFoundException("Volunteer assignment was not found.");
            if (assignment.Status == AssignmentStatus.Cancelled) throw new InvalidOperationException("Assignment is already cancelled.");
            VolunteerShiftRecord? shift = await db.VolunteerShifts.FindAsync([assignmentId], cancellationToken);
            assignment.Status = AssignmentStatus.Cancelled;
            assignment.CancelledAt = DateTime.UtcNow;
            if (shift is not null)
            {
                shift.Status = VolunteerShiftStatus.Cancelled;
                shift.CancelledAt = assignment.CancelledAt;
                db.VolunteerShiftNotifications.Add(Notification(shift, VolunteerShiftNotificationType.Cancelled));
            }
            await db.SaveChangesAsync(cancellationToken);
            schedulePersistenceService.NotifyScheduleChanged(assignment.VolunteerId);
        }

        private async Task<IReadOnlyList<VolunteerApplication>> GetApplicationsAsync(Func<VolunteerApplicationRecord, bool> predicate, CancellationToken cancellationToken)
        {
            await using ApplicationDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
            return (await db.VolunteerApplications.AsNoTracking().OrderByDescending(item => item.AppliedAt).ToListAsync(cancellationToken))
                .Where(predicate).Select(ToDomain).ToList().AsReadOnly();
        }
        private async Task ChangeOpportunityStatusAsync(Guid id, Action<VolunteerOpportunity> change, CancellationToken cancellationToken)
        {
            await using ApplicationDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
            VolunteerOpportunityRecord record = await FindOpportunityAsync(db, id, cancellationToken);
            VolunteerOpportunity opportunity = ToDomain(record);
            change(opportunity); Copy(opportunity, record);
            await db.SaveChangesAsync(cancellationToken);
        }
        private static async Task<VolunteerOpportunityRecord> FindOpportunityAsync(ApplicationDbContext db, Guid id, CancellationToken token) =>
            await db.VolunteerOpportunities.FindAsync([id], token) ?? throw new KeyNotFoundException("Volunteer opportunity was not found.");
        private static async Task<VolunteerApplicationRecord> FindApplicationAsync(ApplicationDbContext db, Guid id, CancellationToken token) =>
            await db.VolunteerApplications.FindAsync([id], token) ?? throw new KeyNotFoundException("Volunteer application was not found.");
        private static VolunteerOpportunity ToDomain(VolunteerOpportunityRecord item) => VolunteerOpportunity.Restore(item.Id, item.Title, item.Description, item.Location, item.StartDateTime, item.EndDateTime, item.RequiredSkills, item.VolunteersNeeded, item.Status, item.CreatedAt);
        private static VolunteerApplication ToDomain(VolunteerApplicationRecord item) => VolunteerApplication.Restore(item.Id, Guid.Parse(item.VolunteerId), item.OpportunityId, item.Status, item.AppliedAt, item.ReviewedAt, Guid.TryParse(item.ReviewedByUserId, out Guid reviewer) ? reviewer : null);
        private static VolunteerAssignment ToDomain(VolunteerAssignmentRecord item) => VolunteerAssignment.Restore(item.Id, Guid.Parse(item.VolunteerId), item.OpportunityId, item.ApplicationId, Guid.Parse(item.AssignedByUserId), item.AssignedAt, item.Status);
        private static VolunteerOpportunityRecord ToRecord(VolunteerOpportunity item) => new() { Id = item.Id, Title = item.Title, Description = item.Description, Location = item.Location, StartDateTime = Local(item.StartDateTime), EndDateTime = Local(item.EndDateTime), RequiredSkills = item.RequiredSkills, VolunteersNeeded = item.VolunteersNeeded, Status = item.Status, CreatedAt = Utc(item.CreatedAt) };
        private static void Copy(VolunteerOpportunity source, VolunteerOpportunityRecord target) { target.Title = source.Title; target.Description = source.Description; target.Location = source.Location; target.StartDateTime = Local(source.StartDateTime); target.EndDateTime = Local(source.EndDateTime); target.RequiredSkills = source.RequiredSkills; target.VolunteersNeeded = source.VolunteersNeeded; target.Status = source.Status; }
        private static VolunteerApplicationRecord ToRecord(VolunteerApplication item) => new() { Id = item.Id, VolunteerId = item.VolunteerId.ToString(), OpportunityId = item.OpportunityId, Status = item.Status, AppliedAt = Utc(item.AppliedAt), ReviewedAt = item.ReviewedAt is null ? null : Utc(item.ReviewedAt.Value), ReviewedByUserId = item.ReviewedByUserId?.ToString() };
        private static VolunteerShiftNotificationRecord Notification(VolunteerShiftRecord shift, VolunteerShiftNotificationType type) => new() { Id = Guid.NewGuid(), VolunteerId = shift.VolunteerId, ShiftId = shift.Id, Type = type, OpportunityTitle = shift.Title, ShiftStartsAt = shift.StartsAt, CreatedAt = DateTime.UtcNow };
        private static DateTime Local(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
        private static DateTime Utc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        private static void ValidateReviewer(User reviewer) { ArgumentNullException.ThrowIfNull(reviewer); if (reviewer.Role != Role.Coordinator && reviewer.Role != Role.Admin) throw new UnauthorizedAccessException("Only coordinators or administrators can review applications."); }
    }
}
