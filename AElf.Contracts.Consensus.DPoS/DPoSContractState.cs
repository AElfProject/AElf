using System;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    public class RoundsMapState : MappedState<UInt64Value, Round>
    {
    }

    public class MinersMapState : MappedState<UInt64Value, Miners>
    {
    }

    public class TicketsMapState : MappedState<StringValue, Tickets>
    {
    }

    public class SnapshotMapState : MappedState<UInt64Value, TermSnapshot>
    {
    }

    public class AliasesMapState : MappedState<StringValue, StringValue>
    {
    }

    public class AliasesLookupMapState : MappedState<StringValue, StringValue>
    {
    }

    public class HistoryMapState : MappedState<StringValue, CandidateInHistory>
    {
    }

    public class AgeToRoundNumberMapState : MappedState<UInt64Value, UInt64Value>
    {
    }

    public class VotingRecordsMapState : MappedState<Hash, VotingRecord>
    {
    }

    public class TermToFirstRoundMapState : MappedState<UInt64Value, UInt64Value>
    {
    }

    // ReSharper disable InconsistentNaming
    public class DividendContractReferenceState : ContractReferenceState
    {
        public Action<ulong> KeepWeights { get; set; }
        public Action<ulong, ulong> SubWeights { get; set; }
        public Action<ulong, ulong> AddWeights { get; set; }
        public Action<ulong, ulong> AddDividends { get; set; }
        public Action<VotingRecord> TransferDividends { get; set; }
        public Action<Address, ulong> SendDividends { get; set; }
    }

    public class TokenContractReferenceState : ContractReferenceState
    {
        public Action<string, string, ulong, uint> Initialize { get; set; }
        public Action<Address, ulong> Transfer { get; set; }
        public Action<Address, ulong> Lock { get; set; }
        public Action<Address, ulong> Unlock { get; set; }
    }

    // ReSharper disable once InconsistentNaming
    public class DPoSContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        public DividendContractReferenceState DividendContract { get; set; }
        public TokenContractReferenceState TokenContract { get; set; }

        /// <summary>
        /// Current round number.
        /// </summary>
        public UInt64State CurrentRoundNumberField { get; set; }

        /// <summary>
        /// Current term number.
        /// </summary>
        public UInt64State CurrentTermNumberField { get; set; }

        /// <summary>
        /// Record the start round of each term.
        /// In Map: term number -> round number
        /// </summary>
        public SingletonState<TermNumberLookUp> TermNumberLookupField { get; set; }

        /// <summary>
        /// Timestamp for genesis of this blockchain.
        /// </summary>
        public SingletonState<Timestamp> BlockchainStartTimestamp { get; set; }

        /// <summary>
        /// The nodes declared join the election for Miners.
        /// </summary>
        public SingletonState<Candidates> CandidatesField { get; set; }

        /// <summary>
        /// Days since we started this blockchain.
        /// </summary>
        public UInt64State AgeField { get; set; }

        /// <summary>
        /// DPoS information of each round.
        /// round number -> round information
        /// </summary>
        public RoundsMapState RoundsMap { get; set; }

        /// <summary>
        /// DPoS mining interval.
        /// </summary>
        public Int32State MiningIntervalField { get; set; }

        /// <summary>
        /// Miners of each term.
        /// term number -> miners
        /// </summary>
        public MinersMapState MinersMap;

        /// <summary>
        /// Tickets of each address (public key).
        /// public key hex value -> tickets information
        /// </summary>
        public TicketsMapState TicketsMap;

        /// <summary>
        /// Snapshots of all terms.
        /// term number -> snapshot
        /// </summary>
        public SnapshotMapState SnapshotMap;

        /// <summary>
        /// Aliases of candidates.
        /// candidate public key hex value -> alias
        /// </summary>
        public AliasesMapState AliasesMap;

        /// <summary>
        /// Aliases of candidates.
        /// alias -> candidate public key hex value
        /// </summary>
        public AliasesLookupMapState AliasesLookupMap;

        /// <summary>
        /// Histories of all candidates
        /// candidate public key hex value -> history information
        /// </summary>
        public HistoryMapState HistoryMap;

        /// <summary>
        /// blockchain age -> first round number.
        /// </summary>
        public AgeToRoundNumberMapState AgeToRoundNumberMap;

        /// <summary>
        /// Keep tracking of the count of votes.
        /// </summary>
        public UInt64State VotesCountField;

        /// <summary>
        /// Keep tracking of the count of tickets.
        /// </summary>
        public UInt64State TicketsCountField;

        /// <summary>
        /// Whether 2/3 of miners mined in current term.
        /// </summary>
        public BoolState TwoThirdsMinersMinedCurrentTermField;

        /// <summary>
        /// Transaction Id -> Voting Record.
        /// </summary>
        public VotingRecordsMapState VotingRecordsMap;

        /// <summary>
        /// Term Number -> First Round Number of this term.
        /// </summary>
        public TermToFirstRoundMapState TermToFirstRoundMap;

        public Int32State ChainIdField;
    }
}