namespace AElf.ContractTestKit.AEDPoSExtension
{
    // ReSharper disable once InconsistentNaming
    public static class AEDPoSExtensionConstants
    {
        public const int InitialKeyPairCount = 5;
        public const int CoreDataCenterKeyPairCount = 17;
        public const int ValidationDataCenterKeyPairCount = CoreDataCenterKeyPairCount * 5;

        public static readonly int CitizenKeyPairsCount =
            SampleAccount.Accounts.Count - InitialKeyPairCount - CoreDataCenterKeyPairCount * 6;

        public const int MiningInterval = 4000;
        public const int TinyBlocksNumber = 8;
        public const int ActualMiningInterval = MiningInterval / TinyBlocksNumber;
        public const int PeriodSeconds = 120;
        public const int MinerIncreaseInterval = 240;
    }
}