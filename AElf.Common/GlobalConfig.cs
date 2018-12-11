using System;

namespace AElf.Common
{
    // ReSharper disable InconsistentNaming
    public static class GlobalConfig
    {
        public static string DefaultChainId = "AELF";
        public static string AElfAddressPrefix = "ELF";
        
        public static int ChainIdLength = 3;
        public static int ContractAddressHashLength = 18;
        
        public static int AddressHashLength = 32; // length of sha256
        
        public const ulong GenesisBlockHeight = 1;
        
        public static readonly string GenesisSmartContractZeroAssemblyName = "AElf.Contracts.Genesis";
        public static readonly string GenesisConsensusContractAssemblyName = "AElf.Contracts.Consensus";
        public static readonly string GenesisTokenContractAssemblyName = "AElf.Contracts.Token";
        public static readonly string GenesisCrossChainContractAssemblyName = "AElf.Contracts.CrossChain";
        public static readonly string GenesisAuthorizationContractAssemblyName = "AElf.Contracts.Authorization";
        public static readonly string GenesisResourceContractAssemblyName = "AElf.Contracts.Resource";

        public static readonly ulong ReferenceBlockValidPeriod = 64;

        public static readonly ulong GenesisBasicContract = 0;
        public static readonly ulong ConsensusContract = 1;
        public static readonly ulong TokenContract = 2;
        public static readonly ulong CrossChainContract = 3;
        public static readonly ulong AuthorizationContract = 4;
        public static readonly ulong ResourceContract = 5;
        public static readonly ulong DividendsContract = 5;

        public static int InvertibleChainHeight = 4;

        public static int BlockProducerNumber = 17;
        public static int BlockNumberOfEachRound = 18;

        #region AElf DPoS

        public const ulong LockTokenForElection = 100_000;
        public const ulong DaysEachTerm = 7;
        public const ulong MaxMissedTimeSlots = 1024;
        public const int AElfDPoSLogRoundCount = 1;
        public static int AElfDPoSMiningInterval = 4000;
        public static readonly int AElfMiningInterval = AElfDPoSMiningInterval * 9 / 10;
        public const int ProducerRepetitions = 8;
        public const int AElfWaitFirstRoundTime = 8000;
        public const string AElfDPoSCurrentRoundNumber = "__AElfCurrentRoundNumber__";
        public const string AElfDPoSOngoingMinersString = "__AElfBlockProducer__";
        public const string AElfDPoSRoundsMapString = "__AElfDPoSRoundsMapString__";
        public const string AElfDPoSExtraBlockProducerString = "__AElfExtraBlockProducer__";
        public const string AElfDPoSExtraBlockTimeSlotString = "__AElfExtraBlockTimeSlot__";
        public const string AElfDPoSAgeFieldString = "__AElfDPoSAgeFieldString__";
        public const string AElfDPoSMiningIntervalString = "__AElfDPoSMiningInterval__";
        public const string AElfDPoSTicketsMapString = "__AElfDPoSTicketsMapString__";
        public const string AElfDPoSCandidatesString = "__AElfDPoSCandidatesString__";
        public const string AElfDPoSSnapshotFieldString = "__AElfDPoSSnapshotFieldString__";
        public const string AElfDPoSDividendsMapString = "__AElfDPoSDividendsMapString__";
        public const string AElfDPoSAliasesMapString = "__AElfDPoSAliasesMapString__";
        public const string AElfDPoSTermLookUpString = "__AElfDPoSTermLookUpString__";
        public const string AElfVotingRecordsString = "__AElfVotingRecordsString__";
        public const string AElfDPoSHistoryMapString = "__AElfDPoSHistoryMapString__";
        public const string AElfDPoSCurrentTermNumber = "__AElfDPoSCurrentTermNumber__";
        public const string AElfDPoSBlockchainStartTimestamp = "__AElfDPoSBlockchainStartTimestamp__";

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

        #region Dividends

        public const ulong ElfTokenPerBlock = 1;
        public const double DividendsForEveryMiner = 0.4;
        public const double DividendsForTicketsCount = 0.1;
        public const double DividendsForReappointment = 0.1;
        public const double DividendsForBackupNodes = 0.2;
        public const double DividendsForVoters = 0.2;
        public const string DividendsMapString = "__DividendsMapString__";
        public const string WeightsMapString = "__WeightsMapString__";
        public const string TotalWeightsMapString = "__TotalWeightsMapString__";
        public const string TransferMapString = "__TransferMapString__";

        #endregion

        public static ulong BasicContractZeroSerialNumber = 100;

        #region data key prefixes

        public const string StatePrefix = "st";
        public const string TransactionReceiptPrefix = "rc";

        #endregion data key prefixes
        public static ulong BlockCacheLimit = 64; 
        
        public const ulong ForkDetectionLength = 4;
    }
}