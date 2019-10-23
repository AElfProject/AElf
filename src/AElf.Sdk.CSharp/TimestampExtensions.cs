using Google.Protobuf.WellKnownTypes;

namespace AElf.Sdk.CSharp
{
    public static class TimestampExtensions
    {
        public static Timestamp AddMilliseconds(this Timestamp timestamp, long milliseconds)
        {
            return timestamp + new Duration
                       {Seconds = milliseconds / 1000, Nanos = (int) (milliseconds % 1000).Mul(1000000)};
        }

        public static Timestamp AddSeconds(this Timestamp timestamp, long seconds)
        {
            return timestamp + new Duration {Seconds = seconds};
        }

        public static Timestamp AddMinutes(this Timestamp timestamp, long minutes)
        {
            return timestamp + new Duration {Seconds = minutes.Mul(60)};
        }

        public static Timestamp AddHours(this Timestamp timestamp, long hours)
        {
            return timestamp + new Duration {Seconds = hours.Mul(60 * 60)};
        }

        public static Timestamp AddDays(this Timestamp timestamp, long days)
        {
            return timestamp + new Duration {Seconds = days.Mul(24 * 60 * 60)};
        }

        public static long Milliseconds(this Duration duration)
        {
            return duration.Seconds > long.MaxValue.Div(1000)
                ? long.MaxValue
                : duration.Seconds.Mul(1000).Add(duration.Nanos.Div(1000000));
        }
    }
}