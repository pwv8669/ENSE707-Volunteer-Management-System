using System.Globalization;

namespace WebApp.Services.Profile
{
    // Shared date and number formatting for the profile page, so every section reads the same way.
    public static class ProfileFormat
    {
        // e.g. "Sat 5 Oct 2026"
        public static string Date(DateTime value) =>
            value.ToString("ddd d MMM yyyy", CultureInfo.CurrentCulture);

        // e.g. "9:30 am"
        public static string Time(DateTime value) =>
            value.ToString("h:mm tt", CultureInfo.CurrentCulture).ToLowerInvariant();

        // e.g. "Sat 5 Oct 2026, 9:00 am – 1:00 pm", or both dates when the window spans days.
        public static string Range(DateTime start, DateTime end)
        {
            if (start.Date == end.Date)
            {
                return $"{Date(start)}, {Time(start)} – {Time(end)}";
            }

            return $"{Date(start)}, {Time(start)} – {Date(end)}, {Time(end)}";
        }

        // e.g. "4.5 hrs", "1 hr", "0 hrs"
        public static string Hours(double hours)
        {
            string number = hours.ToString("0.#", CultureInfo.CurrentCulture);
            return hours == 1 ? $"{number} hr" : $"{number} hrs";
        }
    }
}
