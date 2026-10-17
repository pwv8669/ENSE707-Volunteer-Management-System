using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Volunteer_Management_System;

namespace WebApp.Data
{
    // The app's Entity Framework database context: Identity's tables plus the volunteer profile tables.
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        // Availability windows volunteers add on their profile.
        public DbSet<VolunteerAvailabilitySlot> VolunteerAvailabilitySlots => Set<VolunteerAvailabilitySlot>();

        // Skills and interests volunteers list on their profile.
        public DbSet<VolunteerProfileTag> VolunteerProfileTags => Set<VolunteerProfileTag>();

        // Assigned shifts and their notifications are persisted for Feature 5.
        public DbSet<VolunteerShiftRecord> VolunteerShifts => Set<VolunteerShiftRecord>();

        public DbSet<VolunteerShiftNotificationRecord> VolunteerShiftNotifications =>
            Set<VolunteerShiftNotificationRecord>();

        // Attendance outcomes and completed hours are persisted for Feature 6.
        public DbSet<VolunteerAttendanceRecord> VolunteerAttendanceRecords =>
            Set<VolunteerAttendanceRecord>();

        // Features 2 to 4 use these records as their durable source of truth.
        public DbSet<VolunteerOpportunityRecord> VolunteerOpportunities =>
            Set<VolunteerOpportunityRecord>();

        public DbSet<VolunteerApplicationRecord> VolunteerApplications =>
            Set<VolunteerApplicationRecord>();

        public DbSet<VolunteerAssignmentRecord> VolunteerAssignments =>
            Set<VolunteerAssignmentRecord>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Identity's own tables must be configured first.
            base.OnModelCreating(builder);

            builder.Entity<VolunteerAvailabilitySlot>(slot =>
            {
                slot.HasKey(item => item.Id);
                slot.Property(item => item.UserId).IsRequired();

                // Local wall-clock times, so no time zone conversion happens on save or load.
                slot.Property(item => item.StartsAt).HasColumnType("timestamp without time zone");
                slot.Property(item => item.EndsAt).HasColumnType("timestamp without time zone");
                slot.Property(item => item.Note).HasMaxLength(ProfileFieldLimits.AvailabilityNote);

                // Slots are always loaded per volunteer in time order.
                slot.HasIndex(item => new { item.UserId, item.StartsAt });

                // Deleting an account deletes its availability too.
                slot.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(item => item.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<VolunteerProfileTag>(tag =>
            {
                tag.HasKey(item => item.Id);
                tag.Property(item => item.UserId).IsRequired();
                tag.Property(item => item.Value).IsRequired().HasMaxLength(ProfileTagRules.MaxTagLength);

                // Saved as "Skill" / "Interest" so the table is readable in Supabase.
                tag.Property(item => item.Kind).HasConversion<string>().HasMaxLength(20);

                tag.HasIndex(item => new { item.UserId, item.Kind, item.Value }).IsUnique();

                tag.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(item => item.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<VolunteerShiftRecord>(shift =>
            {
                shift.HasKey(item => item.Id);
                shift.Property(item => item.VolunteerId).IsRequired();
                shift.Property(item => item.Title).IsRequired().HasMaxLength(200);
                shift.Property(item => item.Location).IsRequired().HasMaxLength(300);
                shift.Property(item => item.RequiredSkills).IsRequired().HasMaxLength(500);
                shift.Property(item => item.StartsAt).HasColumnType("timestamp without time zone");
                shift.Property(item => item.EndsAt).HasColumnType("timestamp without time zone");
                shift.Property(item => item.AssignedAt).HasColumnType("timestamp with time zone");
                shift.Property(item => item.CancelledAt).HasColumnType("timestamp with time zone");
                shift.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
                shift.HasIndex(item => new { item.VolunteerId, item.Status, item.StartsAt });
                shift.HasIndex(item => new { item.OpportunityId, item.Status });

                // Deleting an Identity account also removes its saved schedule.
                shift.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(item => item.VolunteerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<VolunteerShiftNotificationRecord>(notification =>
            {
                notification.HasKey(item => item.Id);
                notification.Property(item => item.VolunteerId).IsRequired();
                notification.Property(item => item.OpportunityTitle).IsRequired().HasMaxLength(200);
                notification.Property(item => item.ShiftStartsAt).HasColumnType("timestamp without time zone");
                notification.Property(item => item.CreatedAt).HasColumnType("timestamp with time zone");
                notification.Property(item => item.Type).HasConversion<string>().HasMaxLength(20);
                notification.HasIndex(item => new { item.VolunteerId, item.IsRead, item.CreatedAt });

                notification.HasOne(item => item.Shift)
                    .WithMany()
                    .HasForeignKey(item => item.ShiftId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<VolunteerAttendanceRecord>(attendance =>
            {
                // A shift can have one attendance outcome. Saving it again
                // updates the existing record instead of creating a duplicate.
                attendance.HasKey(item => item.ShiftId);
                attendance.Property(item => item.VolunteerId).IsRequired();
                attendance.Property(item => item.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20);
                attendance.Property(item => item.HoursCompleted)
                    .HasPrecision(8, 2);
                attendance.Property(item => item.Notes)
                    .IsRequired()
                    .HasMaxLength(500);
                attendance.Property(item => item.RecordedByUserId).IsRequired();
                attendance.Property(item => item.RecordedAt)
                    .HasColumnType("timestamp with time zone");
                attendance.HasIndex(item =>
                    new { item.VolunteerId, item.Status, item.RecordedAt });

                attendance.HasOne(item => item.Shift)
                    .WithOne()
                    .HasForeignKey<VolunteerAttendanceRecord>(item => item.ShiftId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<VolunteerOpportunityRecord>(opportunity =>
            {
                opportunity.HasKey(item => item.Id);
                opportunity.Property(item => item.Title).IsRequired().HasMaxLength(200);
                opportunity.Property(item => item.Description).IsRequired().HasMaxLength(4000);
                opportunity.Property(item => item.Location).IsRequired().HasMaxLength(300);
                opportunity.Property(item => item.RequiredSkills).IsRequired().HasMaxLength(500);
                opportunity.Property(item => item.StartDateTime).HasColumnType("timestamp without time zone");
                opportunity.Property(item => item.EndDateTime).HasColumnType("timestamp without time zone");
                opportunity.Property(item => item.CreatedAt).HasColumnType("timestamp with time zone");
                opportunity.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
                opportunity.HasIndex(item => new { item.Status, item.StartDateTime });
            });

            builder.Entity<VolunteerApplicationRecord>(application =>
            {
                application.HasKey(item => item.Id);
                application.Property(item => item.VolunteerId).IsRequired();
                application.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
                application.Property(item => item.AppliedAt).HasColumnType("timestamp with time zone");
                application.Property(item => item.ReviewedAt).HasColumnType("timestamp with time zone");
                application.HasIndex(item => new { item.VolunteerId, item.OpportunityId }).IsUnique();
                application.HasIndex(item => new { item.OpportunityId, item.Status });
                application.HasOne<VolunteerOpportunityRecord>()
                    .WithMany()
                    .HasForeignKey(item => item.OpportunityId)
                    .OnDelete(DeleteBehavior.Cascade);
                application.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(item => item.VolunteerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<VolunteerAssignmentRecord>(assignment =>
            {
                assignment.HasKey(item => item.Id);
                assignment.Property(item => item.VolunteerId).IsRequired();
                assignment.Property(item => item.AssignedByUserId).IsRequired();
                assignment.Property(item => item.AssignedAt).HasColumnType("timestamp with time zone");
                assignment.Property(item => item.CancelledAt).HasColumnType("timestamp with time zone");
                assignment.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
                assignment.HasIndex(item => new { item.OpportunityId, item.Status });
                assignment.HasIndex(item => new { item.VolunteerId, item.Status });
                assignment.HasOne<VolunteerOpportunityRecord>()
                    .WithMany()
                    .HasForeignKey(item => item.OpportunityId)
                    .OnDelete(DeleteBehavior.Cascade);
                assignment.HasOne<VolunteerApplicationRecord>()
                    .WithMany()
                    .HasForeignKey(item => item.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
