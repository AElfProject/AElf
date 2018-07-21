using AElf.Kernel.Types;

namespace AElf.Kernel
{
    public static class Globals
    {
        public static readonly string GenesisSmartContractZeroAssemblyName = "AElf.Contracts.Genesis";
        public static readonly string GenesisSmartContractLastName = ".ContractZeroWithAElfDPoS";
        public static readonly string SmartContractZeroIdString = "__SmartContractZero__";
        
        public static readonly ConsensusType ConsensusType = ConsensusType.AElfDPoS;

        #region AElf DPoS

        public const int AElfMiningTime = 4000;
        public const int AElfCheckTime = 1000;
        public const int AElfWaitFirstRoundTime = 1000;
        // ReSharper disable once InconsistentNaming
        public const string AElfDPoSCurrentRoundNumber = "AElfCurrentRoundNumber";
        // ReSharper disable once InconsistentNaming
        public const string AElfDPoSBlockProducerString = "AElfBlockProducer";
        // ReSharper disable once InconsistentNaming
        public const string AElfDPoSInformationString = "AElfDPoSInformation";
        // ReSharper disable once InconsistentNaming
        public const string AElfDPoSExtraBlockProducerString = "AElfExtraBlockProducer";
        // ReSharper disable once InconsistentNaming
        public const string AElfDPoSExtraBlockTimeslotString = "AElfExtraBlockTimeslot";
        // ReSharper disable once InconsistentNaming
        public const string AElfDPoSChainCreatorString = "AElfChainCreator";
        // ReSharper disable once InconsistentNaming
        public const string AElfDPoSFirstPlaceOfEachRoundString = "AElfFirstPlaceOfEachRound";

        #endregion
    }
}