using System;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel
{
    public static class TimestampHelper
    {
        public static Timestamp GetUtcNow()
        {
            return DateTime.UtcNow.ToTimestamp();
        }
        
        public static Duration DurationFromMilliseconds(long milliseconds)
        {
            return new Duration { Seconds = milliseconds / 1000, Nanos = (int)(milliseconds % 1000) * 1000000 };
        }
        
        public static Duration DurationFromSeconds(long seconds)
        {
            return new Duration { Seconds = seconds};
        }
        
        public static Duration DurationFromMinutes(long minutes)
        {
            return new Duration { Seconds = minutes * 60};
        }
    }
}
