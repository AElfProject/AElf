using System;

namespace AElf.Common
{
    // ReSharper disable InconsistentNaming
    public static class GlobalConfig
    {
        // current release version
        public static int ProtocolVersion = 1;
        
        public static string DefaultChainId = "AELF";
        public static string AElfAddressPrefix = "ELF";
        
        public static int ChainIdLength = 3;
        public static int ContractAddressHashLength = 18;
        
        public static int AddressHashLength = 30; // length of sha256
        
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
        public const int AliasLimit = 20;
        public static readonly int AElfMiningInterval = AElfDPoSMiningInterval * 9 / 10;
        public const int ProducerRepetitions = 8;
        public const int AElfWaitFirstRoundTime = 8000;
        public const string AElfDPoSCurrentRoundNumber = "__AElfCurrentRoundNumber__";
        public const string AElfDPoSMinersString = "__AElfBlockProducer__";
        public const string AElfDPoSRoundsMapString = "__AElfDPoSRoundsMapString__";
        public const string AElfDPoSMinersMapString = "__AElfDPoSMinersMapString__";
        public const string AElfDPoSAgeFieldString = "__AElfDPoSAgeFieldString__";
        public const string AElfDPoSMiningIntervalString = "__AElfDPoSMiningInterval__";
        public const string AElfDPoSTicketsMapString = "__AElfDPoSTicketsMapString__";
        public const string AElfDPoSCandidatesString = "__AElfDPoSCandidatesString__";
        public const string AElfDPoSTermNumberLookupString = "__AElfDPoSTermNumberLookUpString__";
        public const string AElfDPoSSnapshotMapString = "__AElfDPoSSnapshotFieldString__";
        public const string AElfDPoSDividendsMapString = "__AElfDPoSDividendsMapString__";
        public const string AElfDPoSAliasesMapString = "__AElfDPoSAliasesMapString__";
        public const string AElfDPoSAliasesLookupMapString = "__AElfDPoSAliasesLookupMapString__";
        public const string AElfVotesCountString = "__AElfVotesCountString__";
        public const string AElfTicketsCountString = "__AElfTicketsCountString__";
        public const string AElfDPoSHistoryMapString = "__AElfDPoSHistoryMapString__";
        public const string AElfDPoSCurrentTermNumber = "__AElfDPoSCurrentTermNumber__";
        public const string AElfDPoSBlockchainStartTimestamp = "__AElfDPoSBlockchainStartTimestamp__";

        #endregion

        #region AElf Cross Chain
        public const string AElfTxRootMerklePathInParentChain = "__TxRootMerklePathInParentChain__";
        public const string AElfParentChainBlockInfo = "__ParentChainBlockInfo__";
        public const string AElfSideChainBlockInfo = "__SideChainBlockInfo__";
        public const string AElfBoundParentChainHeight = "__BoundParentChainHeight__";
        public const int AElfInitCrossChainRequestInterval = 4;
        public const string AElfCurrentParentChainHeight = "__CurrentParentChainHeight__";
        public const string AElfCurrentSideChainHeight = "__SideChainHeight__";
        public const string AElfBinaryMerkleTreeForSideChainTxnRoot = "__BinaryMerkleTreeForSideChainTxnRoot__";
        public const int MaximalCountForIndexingParentChainBlock = 256;
        public const int MaximalCountForIndexingSideChainBlock = 1;
        #endregion

        #region Authorization

        public const string AElfMultiSig = "__MultiSig__";
        public const string AElfProposal = "__Proposal__";
        #endregion

        #region Dividends

        public static ulong ElfTokenPerBlock = 100;
        public const double DividendsForEveryMinerRatio = 0.4;
        public const double DividendsForTicketsCountRatio = 0.1;
        public const double DividendsForReappointmentRatio = 0.1;
        public const double DividendsForBackupNodesRatio = 0.2;
        public const double DividendsForVotersRatio = 0.2;
        public const string DividendsMapString = "__DividendsMapString__";
        public const string WeightsMapString = "__WeightsMapString__";
        public const string TotalWeightsMapString = "__TotalWeightsMapString__";
        public const string TransferMapString = "__TransferMapString__";

        #endregion

        public static ulong BasicContractZeroSerialNumber = 100;

        #region data key prefixes

        public const string TransactionTracePrefix = "a";        
        public const string BlockBodyPrefix = "b";
        public const string SmartContractPrefix = "c";
        public const string TransactionReceiptPrefix = "e";
        public const string GenesisBlockHashPrefix = "g";
        public const string BlockHeaderPrefix = "h";
        public const string MerkleTreePrefix = "k";
        public const string TransactionResultPrefix = "l";
        public const string MetadataPrefix = "m";
        public const string ChianHeightPrefix = "n";
        public const string CanonicalPrefix = "o";
        public const string MinersPrefix = "p";
        public const string CurrentBlockHashPrefix = "r";
        public const string CallGraphPrefix = "i";
        public const string StatePrefix = "s";
        public const string TransactionPrefix = "t";

        #endregion data key prefixes
        
        public static ulong BlockCacheLimit = 2048; 

        #region Consensus Error String
        
        public const string TicketsNotFound = "Tickets not found.";
        public const string CandidateNotFound = "Candidate not found.";
        public const string TermNumberNotFound = "Term number not found.";
        public const string TermSnapshotNotFound = "Term snapshot not found.";
        public const string TermNumberLookupNotFound = "Term number lookup not found.";
        public const string RoundNumberNotFound = "Round information not found.";
        public const string TargetNotAnnounceElection = "Target didn't announce election.";
        public const string CandidateCannotVote = "Candidate can't vote.";
        public const string LockDayIllegal = "Lock days is illegal.";
        public const string RoundIdNotMatched = "Round Id not matched.";
        public const string InValueNotMatchToOutValue = "In Value not match to Out Value.";
        public const string OutValueIsNull = "Out Value is null.";
        public const string SignatureIsNull = "Signature is null.";
        public const string VoterCannotAnnounceElection = "Voter can't announce election.";

        #endregion
    }
}