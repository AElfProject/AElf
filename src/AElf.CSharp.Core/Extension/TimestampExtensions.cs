using Google.Protobuf.WellKnownTypes;

namespace AElf.CSharp.Core.Extension
{
    /// <summary>
    /// Helper methods for dealing with protobuf timestamps.
    /// </summary>
    public static class TimestampExtensions
    {
        /// <summary>
        /// Adds a given amount of milliseconds to a timestamp. Returns a new instance.
        /// </summary>
        /// <param name="timestamp">the timestamp.</param>
        /// <param name="milliseconds">the amount of milliseconds to add.</param>
        /// <returns>a new timestamp instance.</returns>
        public static Timestamp AddMilliseconds(this Timestamp timestamp, long milliseconds)
        {
            return timestamp + new Duration
                       {Seconds = milliseconds / 1000, Nanos = (int) (milliseconds % 1000).Mul(1000000)};
        }

        /// <summary>
        /// Adds a given amount of seconds to a timestamp. Returns a new instance.
        /// </summary>
        /// <param name="timestamp">the timestamp.</param>
        /// <param name="seconds">the amount of seconds.</param>
        /// <returns>a new timestamp instance.</returns>
        public static Timestamp AddSeconds(this Timestamp timestamp, long seconds)
        {
            return timestamp + new Duration {Seconds = seconds};
        }

        /// <summary>
        /// Adds a given amount of minutes to a timestamp. Returns a new instance.
        /// </summary>
        /// <param name="timestamp">the timestamp.</param>
        /// <param name="minutes">the amount of minutes.</param>
        /// <returns>a new timestamp instance.</returns>
        public static Timestamp AddMinutes(this Timestamp timestamp, long minutes)
        {
            return timestamp + new Duration {Seconds = minutes.Mul(60)};
        }

        /// <summary>
        /// Adds a given amount of hours to a timestamp. Returns a new instance.
        /// </summary>
        /// <param name="timestamp">the timestamp.</param>
        /// <param name="hours">the amount of hours.</param>
        /// <returns>a new timestamp instance.</returns>
        public static Timestamp AddHours(this Timestamp timestamp, long hours)
        {
            return timestamp + new Duration {Seconds = hours.Mul(60 * 60)};
        }

        /// <summary>
        /// Adds a given amount of days to a timestamp. Returns a new instance.
        /// </summary>
        /// <param name="timestamp">the timestamp.</param>
        /// <param name="days">the amount of days.</param>
        /// <returns>a new timestamp instance.</returns>
        public static Timestamp AddDays(this Timestamp timestamp, long days)
        {
            return timestamp + new Duration {Seconds = days.Mul(24 * 60 * 60)};
        }

        /// <summary>
        /// Converts a protobuf duration to long.
        /// </summary>
        /// <param name="duration">the duration to convert.</param>
        /// <returns>the duration represented with a long.</returns>
        public static long Milliseconds(this Duration duration)
        {
            return duration.Seconds > long.MaxValue.Div(1000)
                ? long.MaxValue
                : duration.Seconds.Mul(1000).Add(duration.Nanos.Div(1000000));
        }

        /// <summary>
        /// Compares two timestamps and returns the greater one.
        /// </summary>
        /// <param name="timestamp1">the first timestamp</param>
        /// <param name="timestamp2">the second timestamp</param>
        /// <returns>the greater timestamp.</returns>
        public static Timestamp Max(Timestamp timestamp1, Timestamp timestamp2)
        {
            return timestamp1 > timestamp2 ? timestamp1 : timestamp2;
        }
    }
}