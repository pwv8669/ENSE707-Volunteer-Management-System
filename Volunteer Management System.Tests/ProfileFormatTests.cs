using System.Globalization;
using WebApp.Services.Profile;

// This class contains unit tests for the WebApp's ProfileFormat class, which formats dates, times, ranges and hours for the profile page.
namespace Volunteer_Management_System.Tests
{
    [TestClass]
    public class ProfileFormatTests
    {
        // Test that a date is shown as a short weekday, day, month and year.
        [TestMethod]
        public void Date_FormatsWeekdayDayMonthAndYear()
        {
            WithInvariantCulture(() =>
                Assert.AreEqual("Sat 10 Oct 2026", ProfileFormat.Date(new DateTime(2026, 10, 10))));
        }

        // Test that a morning time is shown in lower-case am without a leading zero.
        [TestMethod]
        public void Time_InTheMorning_UsesLowerCaseAm()
        {
            WithInvariantCulture(() =>
                Assert.AreEqual("9:30 am", ProfileFormat.Time(new DateTime(2026, 10, 10, 9, 30, 0))));
        }

        // Test that an afternoon time is shown in lower-case pm on a 12-hour clock.
        [TestMethod]
        public void Time_InTheAfternoon_UsesLowerCasePm()
        {
            WithInvariantCulture(() =>
                Assert.AreEqual("1:05 pm", ProfileFormat.Time(new DateTime(2026, 10, 10, 13, 5, 0))));
        }

        // Test the boundaries of the clock: midnight is 12:00 am and noon is 12:00 pm.
        [TestMethod]
        public void Time_AtMidnightAndNoon_Uses12HourBoundaries()
        {
            WithInvariantCulture(() =>
            {
                Assert.AreEqual("12:00 am", ProfileFormat.Time(new DateTime(2026, 10, 10, 0, 0, 0)));
                Assert.AreEqual("12:00 pm", ProfileFormat.Time(new DateTime(2026, 10, 10, 12, 0, 0)));
            });
        }

        // Test that a window inside one day shows the date once, then the start and end times.
        [TestMethod]
        public void Range_OnTheSameDay_ShowsTheDateOnce()
        {
            WithInvariantCulture(() =>
            {
                DateTime start = new(2026, 10, 10, 9, 0, 0);
                DateTime end = new(2026, 10, 10, 13, 0, 0);

                Assert.AreEqual(
                    "Sat 10 Oct 2026, 9:00 am – 1:00 pm",
                    ProfileFormat.Range(start, end));
            });
        }

        // Test that a window crossing midnight shows the date for both the start and the end.
        [TestMethod]
        public void Range_AcrossTwoDays_ShowsBothDates()
        {
            WithInvariantCulture(() =>
            {
                DateTime start = new(2026, 10, 10, 22, 0, 0);
                DateTime end = new(2026, 10, 11, 2, 0, 0);

                Assert.AreEqual(
                    "Sat 10 Oct 2026, 10:00 pm – Sun 11 Oct 2026, 2:00 am",
                    ProfileFormat.Range(start, end));
            });
        }

        // Test that whole hours are shown without a decimal point.
        [TestMethod]
        public void Hours_WithWholeNumber_ShowsNoDecimal()
        {
            WithInvariantCulture(() =>
                Assert.AreEqual("3 hrs", ProfileFormat.Hours(3)));
        }

        // Test that exactly one hour uses the singular "hr".
        [TestMethod]
        public void Hours_WithExactlyOne_UsesSingularHr()
        {
            WithInvariantCulture(() =>
                Assert.AreEqual("1 hr", ProfileFormat.Hours(1)));
        }

        // Test that zero hours is shown as "0 hrs" (boundary: no activity yet).
        [TestMethod]
        public void Hours_WithZero_ShowsZeroHrs()
        {
            WithInvariantCulture(() =>
                Assert.AreEqual("0 hrs", ProfileFormat.Hours(0)));
        }

        // Test that a half hour is shown with one decimal place.
        [TestMethod]
        public void Hours_WithHalfHour_ShowsOneDecimal()
        {
            WithInvariantCulture(() =>
                Assert.AreEqual("4.5 hrs", ProfileFormat.Hours(4.5)));
        }

        // Test that long decimals are rounded to one decimal place.
        [TestMethod]
        public void Hours_WithLongDecimal_RoundsToOneDecimal()
        {
            WithInvariantCulture(() =>
                Assert.AreEqual("2.3 hrs", ProfileFormat.Hours(2.26)));
        }

        // Runs a check with the invariant culture so the expected text doesn't depend on this computer's regional settings.
        private static void WithInvariantCulture(Action check)
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                check();
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }
    }
}
