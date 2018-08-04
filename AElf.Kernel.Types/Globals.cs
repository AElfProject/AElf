using AElf.Kernel.Types;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public static class Globals
    {
        public static readonly string GenesisSmartContractZeroAssemblyName = "AElf.Contracts.Genesis";
        public static readonly string GenesisSmartContractLastName = ".ContractZeroWithAElfDPoS";
        public static readonly string GenesisSmartContractZeroUserAvailable= "UserAvailable";
        public static readonly string SmartContractZeroIdString = "__SmartContractZero__";
        
        public static ConsensusType ConsensusType = ConsensusType.AElfDPoS;
        public static int BlockProducerNumber = 0;

        #region AElf DPoS

        // ReSharper disable once InconsistentNaming
        public const int AElfDPoSLogRoundCount = 3;
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
        public const string AElfDPoSFirstPlaceOfEachRoundString = "AElfFirstPlaceOfEachRound";

        #endregion

        #region PoTC

        public static int ExpectedTransanctionCount = 8000;

        #endregion
    }
}