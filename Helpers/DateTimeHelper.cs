namespace BakeryOrderSystem.Helpers
{
    public static class DateTimeHelper
    {
        public static DateTime KazanNow()
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        }
    }
}