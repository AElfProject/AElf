using System;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable ClassNeverInstantiated.Global
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
        public MappedState<UInt64Value, Round> RoundsMap { get; set; }

        /// <summary>
        /// DPoS mining interval.
        /// </summary>
        public Int32State MiningIntervalField { get; set; }

        /// <summary>
        /// Miners of each term.
        /// term number -> miners
        /// </summary>
        public MappedState<UInt64Value, Miners> MinersMap;

        /// <summary>
        /// Tickets of each address (public key).
        /// public key hex value -> tickets information
        /// </summary>
        public MappedState<StringValue, Tickets> TicketsMap;

        /// <summary>
        /// Snapshots of all terms.
        /// term number -> snapshot
        /// </summary>
        public MappedState<UInt64Value, TermSnapshot> SnapshotMap;

        /// <summary>
        /// Aliases of candidates.
        /// candidate public key hex value -> alias
        /// </summary>
        public MappedState<StringValue, StringValue> AliasesMap;

        /// <summary>
        /// Aliases of candidates.
        /// alias -> candidate public key hex value
        /// </summary>
        public MappedState<StringValue, StringValue> AliasesLookupMap;

        /// <summary>
        /// Histories of all candidates
        /// candidate public key hex value -> history information
        /// </summary>
        public MappedState<StringValue, CandidateInHistory> HistoryMap;

        /// <summary>
        /// blockchain age -> first round number.
        /// </summary>
        public MappedState<UInt64Value, UInt64Value> AgeToRoundNumberMap;

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
        public MappedState<Hash, VotingRecord> VotingRecordsMap;

        /// <summary>
        /// Term Number -> First Round Number of this term.
        /// </summary>
        public MappedState<UInt64Value, UInt64Value> TermToFirstRoundMap;

        public Int32State ChainIdField;
    }
}