using System;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class VotingRecord
    {
        public ulong Weight => CalculateWeight(Count, LockDaysList[0]);

        public static ulong CalculateWeight(ulong ticketsAmount, int lockTime)
        {
            return (ulong) (lockTime / 270 + 2 / 3) * ticketsAmount;
        }

        public ulong GetDurationDays(ulong currentAge)
        {
            var days = currentAge - VoteAge + 1;
            ulong totalLockDays = 0;
            foreach (var d in LockDaysList)
            {
                totalLockDays += (ulong) d;
            }

            return Math.Min(days, totalLockDays);
        }
        
        public bool IsExpired(ulong currentAge)
        {
            var lockExpiredAge = VoteAge;
            foreach (var day in LockDaysList)
            {
                lockExpiredAge += (ulong) day;
            }

            return lockExpiredAge <= currentAge;
        }
    }
}