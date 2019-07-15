namespace AElf.Contracts.Consensus.AEDPoS
{
    public static class AEDPoSContractConstants
    {
        public const int TinyBlocksNumber = 8;
        public const int TotalTinySlots = TinyBlocksNumber;
        public const int LimitBlockExecutionTimeTotalShares = 5;
        public const int LimitBlockExecutionTimeWeight = 3;
        public const int TimeForNetwork = 100;
        public const long MiningRewardPerBlock = 12500000;
        public const int MinMinersCount = 9;
        public const int RandomNumberRequestMinersCount = 5;
        public const int RandomNumberDueRoundCount = 5;
    }
}