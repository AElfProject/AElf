using AElf.Kernel.Types;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public static class Globals
    {
        public static readonly string GenesisSmartContractZeroAssemblyName = "AElf.Contracts.Genesis";
        public static readonly string GenesisSmartContractLastName = ".ContractZeroWithAElfDPoS";
        public static readonly string SmartContractZeroIdString = "__SmartContractZero__";
        
        public static ConsensusType ConsensusType = ConsensusType.AElfDPoS;
        public static int BlockProducerNumber = 0;
        public const int AElfLogInterval = 1000;

        #region AElf DPoS

        public const int AElfDPoSLogRoundCount = 3;
        public static int AElfDPoSMiningInterval = 4000;
        public const int AElfWaitFirstRoundTime = 1000;
        public const string AElfDPoSCurrentRoundNumber = "AElfCurrentRoundNumber";
        public const string AElfDPoSBlockProducerString = "AElfBlockProducer";
        public const string AElfDPoSInformationString = "AElfDPoSInformation";
        public const string AElfDPoSExtraBlockProducerString = "AElfExtraBlockProducer";
        public const string AElfDPoSExtraBlockTimeslotString = "AElfExtraBlockTimeslot";
        public const string AElfDPoSFirstPlaceOfEachRoundString = "AElfFirstPlaceOfEachRound";
        public const string AElfDPoSMiningIntervalString = "AElfDPoSMiningInterval";

        #endregion

        #region PoTC

        public static ulong ExpectedTransanctionCount = 8000;

        #endregion

        #region Single node test

        public static int SingleNodeTestMiningInterval = 4000;

        #endregion
    }
}