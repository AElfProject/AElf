namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public static class AEDPoSContractConstants
    {
        public const int MaximumTinyBlocksCount = 8;
        public const int RandomNumberDueRoundCount = 1024;
        public const long InitialMiningRewardPerBlock = 12500000;
        public const long TimeToReduceMiningRewardByHalf = 126144000; // 60 * 60 * 24 * 365 * 4
        public const int SupposedMinersCount = 17;
        public const int KeepRounds = 40960;
        public const long TolerableMissedTimeSlotsCount = 30; // one time slot per minute and last 3 days.
    }
}