namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public static class AEDPoSContractConstants
    {
        public const int MaximumTinyBlocksCount = 8;
        public const int RandomNumberDueRoundCount = 1024;
        public const long InitialMiningRewardPerBlock = 12500000;
        public const long TimeToReduceMiningRewardByHalf = 86400; // 60 * 60 * 24 //reduce reward by half every 1 day
        public const int SupposedMinersCount = 5; //initial 5 bps
        public const int KeepRounds = 40960;
        public const long TolerableMissedTimeSlotsCount = 30; // one time slot per minute and last 0.5 hours.
    }
}