using System;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{

    //TODO: move out VotingRecord from AElf.Kernel.Types
    public partial class VotingRecord
    {
        public ulong Weight => CalculateWeight(Count, LockDaysList[0]);

        public static ulong CalculateWeight(ulong ticketsAmount, int lockTime)
        {
            //TODO: use extend method
            return (ulong) (((double) lockTime / 270 + 2.0 / 3.0) * ticketsAmount);
        }
    }
}