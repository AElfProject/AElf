using AElf.Kernel.Types;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public static class Globals
    {
        public static readonly string GenesisSmartContractZeroAssemblyName = "AElf.Contracts.Genesis";
        public static readonly string GenesisConsensusContractAssemblyName = "AElf.Contracts.Consensus";
        public static readonly string GenesisTokenContractAssemblyName = "AElf.Contracts.Token";


        public static readonly string GenesisBasicContract = "BasicContractZero";
        public static readonly string SmartContractZeroIdString = SmartContractType.BasicContractZero.ToString();
        
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