// Purpose:
// Verifies Feature 6 attendance validation, persistence, corrections and
// volunteer participation summaries.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WebApp.Data;
using WebApp.Services.Attendance;

namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class VolunteerAttendanceServiceTests
    {
        // Attendance and completed hours remain available through a new
        // service instance, representing a WebApp restart.
        [TestMethod]
        public async Task RecordAttendanceAsync_NewServiceRetainsHistory()
        {
            TestDbContextFactory factory = CreateFactory();
            VolunteerShiftRecord shift = await SeedShiftAsync(factory);
            VolunteerAttendanceService service = new(factory);

            await service.RecordAttendanceAsync(
                shift.Id,
                Guid.NewGuid().ToString(),
                VolunteerAttendanceStatus.Attended,
                2.5m,
                "Helped with setup and cleanup.");

            VolunteerAttendanceService restartedService = new(factory);
            IReadOnlyList<VolunteerParticipationEntryModel> history =
                await restartedService.GetVolunteerParticipationAsync(
                    shift.VolunteerId);
            VolunteerParticipationSummaryModel summary =
                await restartedService.GetVolunteerSummaryAsync(
                    shift.VolunteerId);

            Assert.HasCount(1, history);
            Assert.AreEqual(VolunteerAttendanceStatus.Attended, history[0].Status);
            Assert.AreEqual(2.5m, history[0].HoursCompleted);
            Assert.AreEqual("Helped with setup and cleanup.", history[0].Notes);
            Assert.AreEqual(1, summary.AttendedShifts);
            Assert.AreEqual(2.5m, summary.TotalHours);
        }

        // Saving the same shift again corrects its one attendance row rather
        // than duplicating the volunteer's participation history.
        [TestMethod]
        public async Task RecordAttendanceAsync_ExistingRecordIsUpdated()
        {
            TestDbContextFactory factory = CreateFactory();
            VolunteerShiftRecord shift = await SeedShiftAsync(factory);
            VolunteerAttendanceService service = new(factory);
            string recorderId = Guid.NewGuid().ToString();
            await service.RecordAttendanceAsync(
                shift.Id,
                recorderId,
                VolunteerAttendanceStatus.Attended,
                2m,
                null);

            await service.RecordAttendanceAsync(
                shift.Id,
                recorderId,
                VolunteerAttendanceStatus.Absent,
                0m,
                "Corrected after coordinator review.");

            await using ApplicationDbContext db = factory.CreateDbContext();
            Assert.AreEqual(1, await db.VolunteerAttendanceRecords.CountAsync());
            VolunteerAttendanceRecord saved =
                await db.VolunteerAttendanceRecords.SingleAsync();
            Assert.AreEqual(VolunteerAttendanceStatus.Absent, saved.Status);
            Assert.AreEqual(0m, saved.HoursCompleted);
        }

        // Present volunteers need positive hours within their assigned shift.
        [TestMethod]
        public async Task RecordAttendanceAsync_InvalidAttendedHoursAreRejected()
        {
            TestDbContextFactory factory = CreateFactory();
            VolunteerShiftRecord shift = await SeedShiftAsync(factory);
            VolunteerAttendanceService service = new(factory);
            string recorderId = Guid.NewGuid().ToString();

            await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() =>
                service.RecordAttendanceAsync(
                    shift.Id,
                    recorderId,
                    VolunteerAttendanceStatus.Attended,
                    0m,
                    null));
            await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() =>
                service.RecordAttendanceAsync(
                    shift.Id,
                    recorderId,
                    VolunteerAttendanceStatus.Attended,
                    4m,
                    null));
        }

        // An absence cannot contribute completed volunteer hours.
        [TestMethod]
        public async Task RecordAttendanceAsync_AbsentHoursAreRejected()
        {
            TestDbContextFactory factory = CreateFactory();
            VolunteerShiftRecord shift = await SeedShiftAsync(factory);
            VolunteerAttendanceService service = new(factory);

            await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() =>
                service.RecordAttendanceAsync(
                    shift.Id,
                    Guid.NewGuid().ToString(),
                    VolunteerAttendanceStatus.Absent,
                    1m,
                    null));
        }

        // Cancelled assignments are retained for audit history but cannot be
        // given a new attendance outcome.
        [TestMethod]
        public async Task RecordAttendanceAsync_CancelledShiftIsRejected()
        {
            TestDbContextFactory factory = CreateFactory();
            VolunteerShiftRecord shift = await SeedShiftAsync(
                factory,
                VolunteerShiftStatus.Cancelled);
            VolunteerAttendanceService service = new(factory);

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                service.RecordAttendanceAsync(
                    shift.Id,
                    Guid.NewGuid().ToString(),
                    VolunteerAttendanceStatus.Absent,
                    0m,
                    null));
        }

        // The management query includes unrecorded shifts so every assignment
        // can receive an attendance result.
        [TestMethod]
        public async Task GetShiftsForAttendanceAsync_IncludesUnrecordedShift()
        {
            TestDbContextFactory factory = CreateFactory();
            VolunteerShiftRecord shift = await SeedShiftAsync(factory);
            VolunteerAttendanceService service = new(factory);

            IReadOnlyList<AttendanceShiftModel> shifts =
                await service.GetShiftsForAttendanceAsync();

            Assert.HasCount(1, shifts);
            Assert.AreEqual(shift.Id, shifts[0].ShiftId);
            Assert.IsNull(shifts[0].AttendanceStatus);
            Assert.AreEqual(0m, shifts[0].HoursCompleted);
        }

        private static async Task<VolunteerShiftRecord> SeedShiftAsync(
            TestDbContextFactory factory,
            VolunteerShiftStatus status = VolunteerShiftStatus.Assigned)
        {
            DateTime startsAt = DateTime.Now.AddDays(-1);
            VolunteerShiftRecord shift = new()
            {
                Id = Guid.NewGuid(),
                VolunteerId = Guid.NewGuid().ToString(),
                OpportunityId = Guid.NewGuid(),
                ApplicationId = Guid.NewGuid(),
                AssignedByUserId = Guid.NewGuid(),
                Title = "Community Garden",
                Location = "AUT Community Garden",
                RequiredSkills = "Gardening",
                StartsAt = startsAt,
                EndsAt = startsAt.AddHours(3),
                AssignedAt = DateTime.UtcNow.AddDays(-2),
                Status = status,
                CancelledAt = status == VolunteerShiftStatus.Cancelled
                    ? DateTime.UtcNow
                    : null
            };

            await using ApplicationDbContext db = factory.CreateDbContext();
            db.VolunteerShifts.Add(shift);
            await db.SaveChangesAsync();
            return shift;
        }

        private static TestDbContextFactory CreateFactory()
        {
            InMemoryDatabaseRoot root = new();
            DbContextOptions<ApplicationDbContext> options =
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString(), root)
                    .Options;
            return new TestDbContextFactory(options);
        }

        private sealed class TestDbContextFactory(
            DbContextOptions<ApplicationDbContext> options)
            : IDbContextFactory<ApplicationDbContext>
        {
            public ApplicationDbContext CreateDbContext() => new(options);

            public Task<ApplicationDbContext> CreateDbContextAsync(
                CancellationToken cancellationToken = default) =>
                Task.FromResult(CreateDbContext());
        }
    }
}
