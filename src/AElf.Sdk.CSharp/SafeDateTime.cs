using System;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Sdk.CSharp
{
    public class SafeDateTime
    {
        private DateTime _datetime;

        public long Ticks => _datetime.Ticks;

        public SafeDateTime(DateTime datetime)
        {
            _datetime = datetime.ToUniversalTime();
        }

        public SafeDateTime(Timestamp timestamp)
        {
            // DateTime from timestamp is already universal time
            _datetime = timestamp.ToDateTime();
        }

        public SafeDateTime AddMilliseconds(long milliseconds)
        {
            checked
            {
                _datetime = _datetime.AddTicks(milliseconds * TimeSpan.TicksPerMillisecond);
            }

            return this;
        }
        
        public SafeDateTime AddSeconds(long seconds)
        {
            checked
            {
                _datetime = _datetime.AddTicks(seconds * TimeSpan.TicksPerSecond);
            }

            return this;
        }
        
        public SafeDateTime AddMinutes(long minutes)
        {
            checked
            {
                _datetime = _datetime.AddTicks(minutes * TimeSpan.TicksPerMinute);
            }

            return this;
        }
        
        public SafeDateTime AddHours(long hours)
        {
            checked
            {
                _datetime = _datetime.AddTicks(hours * TimeSpan.TicksPerHour);
            }

            return this;
        }
        
        public SafeDateTime AddDays(long days)
        {
            checked
            {
                _datetime = _datetime.AddTicks(days * TimeSpan.TicksPerDay);
            }

            return this;
        }

        public DateTime ToDateTime()
        {
            return _datetime;
        }

        public Timestamp ToTimestamp()
        {
            return _datetime.ToTimestamp();
        }
        
        public static SafeTimeSpan operator - (SafeDateTime d1, DateTime d2)
        {
            return new SafeTimeSpan(d1.Ticks - d2.ToUniversalTime().Ticks);
        }

        public static SafeTimeSpan operator - (SafeDateTime d1, Timestamp d2)
        {
            return new SafeTimeSpan(d1.Ticks - d2.ToDateTime().Ticks);
        }
        
        public static SafeTimeSpan operator - (SafeDateTime d1, SafeDateTime d2)
        {
            return new SafeTimeSpan(d1.Ticks - d2.Ticks);
        }
        
        public static SafeTimeSpan operator - (SafeDateTime d1, SafeTimeSpan d2)
        {
            return new SafeTimeSpan(d1.Ticks - d2.Ticks);
        }
    }

    public class SafeTimeSpan
    {
        public const long MsPerSecond = 1000;
        
        public const long MsPerMinute = 1000 * 60;
        
        public const long MsPerHour = 1000 * 60 * 60;

        public const long MsPerDay = 1000 * 60 * 60 * 24;

        public long Ticks { get; }
        
        // Allow access to only TotalMilliseconds and let contract developer decide precision if day, hour, minute needed
        public long TotalMilliseconds => Ticks / TimeSpan.TicksPerMillisecond;
        //public long TotalSeconds => Ticks / TimeSpan.TicksPerSecond;
        //public long TotalMinutes => Ticks / TimeSpan.TicksPerMinute;
        //public long TotalHours => Ticks / TimeSpan.TicksPerHour;
        //public long TotalDays => Ticks / TimeSpan.TicksPerDay;

        public SafeTimeSpan(long ticks)
        {
            Ticks = ticks;
        }

        public static SafeTimeSpan FromMilliseconds(long ms)
        {
            return new SafeTimeSpan(ms * TimeSpan.TicksPerMillisecond);
        }
    }
    
    public static class TimeExtensions
    {
        public static SafeDateTime ToSafeDateTime(this DateTime dateTime)
        {
            return new SafeDateTime(dateTime);
        }

        public static SafeDateTime ToSafeDateTime(this Timestamp timestamp)
        {
            return new SafeDateTime(timestamp);
        }
    }
}