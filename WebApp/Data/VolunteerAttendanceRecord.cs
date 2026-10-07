namespace WebApp.Data
{
    // Stores the attendance outcome and completed hours for one persisted
    // volunteer shift. ShiftId is the primary key, so each shift has at most
    // one attendance record and corrections update the existing row.
    public class VolunteerAttendanceRecord
    {
        public Guid ShiftId { get; set; }

        public string VolunteerId { get; set; } = string.Empty;

        public VolunteerAttendanceStatus Status { get; set; }

        public decimal HoursCompleted { get; set; }

        public string Notes { get; set; } = string.Empty;

        public string RecordedByUserId { get; set; } = string.Empty;

        public DateTime RecordedAt { get; set; }

        public VolunteerShiftRecord Shift { get; set; } = null!;
    }

    public enum VolunteerAttendanceStatus
    {
        Attended,
        Absent
    }
}
