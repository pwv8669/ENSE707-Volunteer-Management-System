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
        }
    }
}
