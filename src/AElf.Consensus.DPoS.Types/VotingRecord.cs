// ReSharper disable once CheckNamespace
namespace AElf.Consensus.DPoS
{
    public partial class VotingRecord
    {
        public long Weight => CalculateWeight(Count, LockDaysList[0]);

        public static long CalculateWeight(long ticketsAmount, int lockTime)
        {
            return (long) (((double) lockTime / 270 + 2.0 / 3.0) * ticketsAmount);
        }
    }
}