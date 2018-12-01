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
        public static readonly string GenesisCrossChainContractAssemblyName = "AElf.Contracts.CrossChain";
        public static readonly string GenesisAuthorizationContractAssemblyName = "AElf.Contracts.Authorization";
        public static readonly string GenesisResourceContractAssemblyName = "AElf.Contracts.Resource";

        public static readonly ulong ReferenceBlockValidPeriod = 64;

        public static readonly UInt64 GenesisBasicContract = 0;
        public static readonly UInt64 ConsensusContract = 1;
        public static readonly UInt64 TokenContract = 2;
        public static readonly UInt64 CrossChainContract = 3;
        public static readonly UInt64 AuthorizationContract = 4;
        public static readonly UInt64 ResourceContract = 5;
        
        public static int InvertibleChainHeight = 4;

        public static int BlockProducerNumber = 17;
        public static int BlockNumberOfEachRound = 18;

        #region AElf DPoS

        public const int AElfDPoSLogRoundCount = 1;
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
        public static readonly int AElfInitCrossChainRequestInterval = 4;
        public const string AElfCurrentParentChainHeight = "__CurrentParentChainHeight__";

        #endregion

        #region Authorization

        public const string AElfMultiSig = "__MultiSig__";
        public const string AElfProposal = "__Proposal__";
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