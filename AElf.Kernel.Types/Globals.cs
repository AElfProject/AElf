namespace AElf.Kernel
{
    public static class Globals
    {
        public static readonly string GenesisSmartContractZeroAssemblyName = "AElf.Contracts.Genesis";
        public static readonly string GenesisSmartContractLastName = ".ContractZeroWithDPoS";
        public static readonly string SmartContractZeroIdString = "__SmartContractZero__";

        #region DPoS

        public const int MiningTime = 4000;
        public const int CheckTime = 1000;
        public const int WaitFirstRoundTime = 6000;
        // ReSharper disable once InconsistentNaming
        public const string DPoSRoundsCountString = "RoundsCount";
        // ReSharper disable once InconsistentNaming
        public const string DPoSBlockProducerString = "BPs";
        // ReSharper disable once InconsistentNaming
        public const string DPoSInfoString = "DPoSInfo";
        // ReSharper disable once InconsistentNaming
        public const string DPoSExtraBlockProducerString = "EBP";
        // ReSharper disable once InconsistentNaming
        public const string DPoSExtraBlockTimeslotString = "EBTime";
        // ReSharper disable once InconsistentNaming
        public const string DPoSChainCreatorString = "ChainCreator";
        // ReSharper disable once InconsistentNaming
        public const string DPoSFirstPlaceOfEachRoundString = "FirstPlaceOfEachRound";

        #endregion
    }
}