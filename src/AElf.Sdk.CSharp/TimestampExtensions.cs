using Google.Protobuf.WellKnownTypes;

namespace AElf.Sdk.CSharp
{
    public static class TimestampExtensions
    {
        public static Timestamp AddMilliseconds(this Timestamp timestamp, long milliseconds)
        {
            return timestamp + new Duration() { Seconds = milliseconds / 1000, Nanos = (int)(milliseconds % 1000).Mul(1000000) };
        }
        
        public static Timestamp AddSeconds(this Timestamp timestamp, long seconds)
        {
            return timestamp + new Duration() { Seconds = seconds };
        }

        public static long Milliseconds(this Duration duration)
        {
            return duration.Seconds.Mul(1000) + duration.Nanos / 1000000;
        }
    }
}