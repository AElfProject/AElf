using System;

namespace AElf.Contracts.Authorization
{
    public static class TimerHelper
    {
        public static DateTime ConvertFromUnixTimestamp(double seconds)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return origin.AddSeconds(seconds);
        }

        public static double ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return diff.TotalSeconds;
        }
    }
}