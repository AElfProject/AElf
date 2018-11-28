using System;

namespace AElf.Common
{
    // ReSharper disable InconsistentNaming
    public static class GlobalConfig
    {
        public static int AddressLength = 18;
        public const ulong GenesisBlockHeight = 1;
        public static readonly string GenesisSmartContractZeroAssemblyName = "AElf.Contracts.Genesis";
        public static readonly string GenesisConsensusContractAssemblyName = "AElf.Contracts.Consensus";
        public static readonly string GenesisTokenContractAssemblyName = "AElf.Contracts.Token";
        public static readonly string GenesisSideChainContractAssemblyName = "AElf.Contracts.SideChain";

        public static readonly ulong ReferenceBlockValidPeriod = 64;

        public static readonly UInt64 GenesisBasicContract = 0;
        public static readonly UInt64 ConsensusContract = 1;
        public static readonly UInt64 TokenContract = 2;
        public static readonly UInt64 SideChainContract = 3;
        
        public static int InvertibleChainHeight = 4;

        public static int BlockProducerNumber = 17;
        public static int BlockNumberOfEachRound = 18;
        public const int AElfLogInterval = 900;

        #region AElf DPoS

        public const ulong LockTokenForElection = 100_000;
        public const int AElfDPoSLogRoundCount = 1;
        public static int AElfDPoSMiningInterval = 4000;
        public static readonly int AElfMiningInterval = AElfDPoSMiningInterval * 9 / 10;
        public const int AElfWaitFirstRoundTime = 8000;
        public const string AElfDPoSCurrentRoundNumber = "__AElfCurrentRoundNumber__";
        public const string AElfDPoSOngoingMinersString = "__AElfBlockProducer__";
        public const string AElfDPoSInformationString = "__AElfDPoSInformation__";
        public const string AElfDPoSExtraBlockProducerString = "__AElfExtraBlockProducer__";
        public const string AElfDPoSExtraBlockTimeSlotString = "__AElfExtraBlockTimeSlot__";
        public const string AElfDPoSFirstPlaceOfEachRoundString = "__AElfFirstPlaceOfEachRound__";
        public const string AElfDPoSMiningIntervalString = "__AElfDPoSMiningInterval__";
        public const string AElfDPoSMiningRoundHashMapString = "__AElfDPoSMiningRoundHashMap__";
        public const string AElfDPoSBalanceMapString = "__AElfDPoSBalanceMapString__";
        public const string AElfDPoSCandidatesString = "__AElfDPoSCandidatesString__";

        #endregion

        #region AElf Cross Chain
        public const string AElfTxRootMerklePathInParentChain = "__TxRootMerklePathInParentChain__";
        public const string AElfParentChainBlockInfo = "__ParentChainBlockInfo__";
        public const string AElfBoundParentChainHeight = "__BoundParentChainHeight__";
        public static readonly int AElfInitCrossChainRequestInterval = AElfDPoSMiningInterval / 1000;
        public const string AElfCurrentParentChainHeight = "__CurrentParentChainHeight__";

        #endregion

        #region Single node test

        public static int SingleNodeTestMiningInterval = 4000;

        #endregion

        public static ulong BasicContractZeroSerialNumber = 100;

        #region data key prefixes

        public const string StatePrefix = "st";
        public const string TransactionReceiptPrefix = "rc";

        #endregion data key prefixes
        public const ulong BlockCacheLimit = 64; 
        
        public const ulong ForkDetectionLength = 4;
    }
}