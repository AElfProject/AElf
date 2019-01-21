using System;

namespace AElf.Kernel
{
    public static class VotingRecordExtensions
    {
        public static ulong GetDurationDays(this VotingRecord votingRecord, ulong currentAge)
        {
            var days = currentAge - votingRecord.VoteAge + 1;
            ulong totalLockDays = 0;
            foreach (var d in votingRecord.LockDaysList)
            {
                totalLockDays += (ulong) d;
            }

            return Math.Min(days, totalLockDays);
        }

        public static bool IsExpired(this VotingRecord votingRecord, ulong currentAge)
        {
            var lockExpiredAge = votingRecord.VoteAge;
            foreach (var day in votingRecord.LockDaysList)
            {
                lockExpiredAge += (ulong) day;
            }

            return lockExpiredAge <= currentAge;
        }
    }
}