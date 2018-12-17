using System;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class VotingRecord
    {
        private TimeSpan PastTime => DateTime.UtcNow - VoteTimestamp.ToDateTime();

        public ulong Weight => (GetCurrentLockingDays() * 10 / 270 + 2 / 3) * Count;

        public ulong DurationDays
        {
            get
            {
                var days = (ulong) ((DateTime.UtcNow - VoteTimestamp.ToDateTime()).TotalDays + 1);
                ulong totalLockDays = 0;
                foreach (var d in LockDaysList)
                {
                    totalLockDays += d;
                }

                return Math.Min(days, totalLockDays);
            }
        } 
        
        public bool IsExpired()
        {
            uint lockDays = 0;
            foreach (var day in LockDaysList)
            {
                lockDays += day;
            }

            return PastTime.TotalDays >= lockDays;
        }

        public uint GetCurrentLockingDays()
        {
            uint lockDays = 0;
            foreach (var day in LockDaysList)
            {
                lockDays += day;
                if (lockDays > PastTime.TotalDays)
                {
                    return day;
                }
            }

            return lockDays;
        }
    }
}