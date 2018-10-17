namespace AElf.Common
{
 public static class GlobalConfig
    {
        public static int AddressLength = 18;
        public static readonly string GenesisSmartContractZeroAssemblyName = "AElf.Contracts.Genesis";
        public static readonly string GenesisConsensusContractAssemblyName = "AElf.Contracts.Consensus";
        public static readonly string GenesisTokenContractAssemblyName = "AElf.Contracts.Token";
        public static readonly string GenesisSideChainContractAssemblyName = "AElf.Contracts.SideChain";

        public static readonly ulong ReferenceBlockValidPeriod = 64;

        public static readonly string GenesisBasicContract = "BasicContractZero";
        
        public static readonly string ConsensusContract = "AElfDPoS";
        
        public static int BlockProducerNumber = 17;
        public static int BlockNumberOfEachRound = 18;
        public const int AElfLogInterval = 900;

        #region AElf DPoS

        public static bool IsConsensusGenerator;
        public const int AElfDPoSLogRoundCount = 1;
        public static int AElfDPoSMiningInterval = 4000;
        public static readonly int AElfMiningInterval = AElfDPoSMiningInterval * 9 / 10;
        public const int AElfWaitFirstRoundTime = 8000;
        public const string AElfDPoSCurrentRoundNumber = "AElfCurrentRoundNumber";
        public const string AElfDPoSBlockProducerString = "AElfBlockProducer";
        public const string AElfDPoSInformationString = "AElfDPoSInformation";
        public const string AElfDPoSExtraBlockProducerString = "AElfExtraBlockProducer";
        public const string AElfDPoSExtraBlockTimeSlotString = "AElfExtraBlockTimeSlot";
        public const string AElfDPoSFirstPlaceOfEachRoundString = "AElfFirstPlaceOfEachRound";
        public const string AElfDPoSMiningIntervalString = "AElfDPoSMiningInterval";
        public const string AElfDPoSMiningRoundHashMapString = "AElfDPoSMiningRoundHashMap";

        #endregion

        #region AElf Cross Chain
        public const string AElfTxRootMerklePathInParentChain = "__TxRootMerklePathInParentChain__";
        public const string AElfParentChainBlockInfo = "__ParentChainBlockInfo__";
        public const string AElfBoundParentChainHeight = "__BoundParentChainHeight__";
        public static readonly int AElfInitCrossChainRequestInterval = AElfDPoSMiningInterval / 1000;
        
        #endregion

        #region PoTC

        public static ulong ExpectedTransactionCount = 8000;

        #endregion

        #region Single node test

        public static int SingleNodeTestMiningInterval = 4000;

        #endregion

        public static ulong BasicContractZeroSerialNumber = 0;
    }
}