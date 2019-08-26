using AElf.Contracts.TestKit;

namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    // ReSharper disable once InconsistentNaming
    public static class AEDPoSExtensionConstants
    {
        public const int InitialKeyPairCount = 5;
        public const int CoreDataCenterKeyPairCount = 9; // Start from 9. AElf Main Chain will start from 17.
        public const int ValidationDataCenterKeyPairCount = CoreDataCenterKeyPairCount * 5;

        public static readonly int CitizenKeyPairsCount =
            SampleECKeyPairs.KeyPairs.Count - InitialKeyPairCount - CoreDataCenterKeyPairCount * 6;

        public const int MiningInterval = 4000;
        public const int TinyBlocksNumber = 8;
        public const int ActualMiningInterval = MiningInterval / TinyBlocksNumber;
        public const int TimeEachTerm = 120;
        public const int MinerIncreaseInterval = 240;
    }
}