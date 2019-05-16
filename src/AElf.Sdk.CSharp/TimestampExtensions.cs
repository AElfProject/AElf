using System;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Sdk.CSharp
{
    public static class TimestampExtensions
    {
        public static Timestamp AddDays(this Timestamp timestamp, long days)
        {
            checked
            {
                return Timestamp.FromDateTimeOffset(
                    DateTimeOffset.FromUnixTimeSeconds(timestamp.Seconds + days * 24 * 60 * 60));
            }
        }
        
        public static Timestamp AddHours(this Timestamp timestamp, long hours)
        {
            checked
            {
                return Timestamp.FromDateTimeOffset(
                    DateTimeOffset.FromUnixTimeSeconds(timestamp.Seconds + hours * 60 * 60));
            }
        }
        
        public static Timestamp AddMinutes(this Timestamp timestamp, long minutes)
        {
            checked
            {
                return Timestamp.FromDateTimeOffset(
                    DateTimeOffset.FromUnixTimeSeconds(timestamp.Seconds + minutes * 60));
            }
        }

        public static long Days(this Duration duration)
        {
            return duration.Seconds / 60 / 60 / 24;
        }

        public static long Hours(this Duration duration)
        {
            return duration.Seconds / 60 / 60;
        }
        
        public static long Minutes(this Duration duration)
        {
            return duration.Seconds / 60;
        }
        
        public static double DecimalDays(this Duration duration)
        {
            return (double) duration.Seconds / 60 / 60 / 24;
        }

        public static double DecimalHours(this Duration duration)
        {
            return (double) duration.Seconds / 60 / 60;
        }
        
        public static double DecimalMinutes(this Duration duration)
        {
            return (double) duration.Seconds / 60;
        }
    }
}