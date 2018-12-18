using System;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class VotingRecord
    {
        public ulong Weight => (LockDaysList[0] / 270 + 2 / 3) * Count;

        public ulong GetDurationDays(ulong currentAge)
        {
            var days = currentAge - VoteAge + 1;
            ulong totalLockDays = 0;
            foreach (var d in LockDaysList)
            {
                totalLockDays += d;
            }

            return Math.Min(days, totalLockDays);
        }
        
        public bool IsExpired(ulong currentAge)
        {
            ulong lockExpiredAge = VoteAge;
            foreach (var day in LockDaysList)
            {
                lockExpiredAge += day;
            }

            return lockExpiredAge >= currentAge;
        }

        public uint GetCurrentLockingDays(ulong currentAge)
        {
            uint lockDays = 0;
            foreach (var day in LockDaysList)
            {
                lockDays += day;
                if (lockDays > currentAge - VoteAge)
                {
                    return day;
                }
            }

            return lockDays;
        }
    }
}