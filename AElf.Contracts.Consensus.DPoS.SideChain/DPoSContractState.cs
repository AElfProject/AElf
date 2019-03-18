using System;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS.SideChain
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable once ClassNeverInstantiated.Global
    public partial class DPoSContractState : ContractState
    {
        public BoolState Initialized { get; set; }


        /// <summary>
        /// Current round number.
        /// </summary>
        public Int64State CurrentRoundNumberField { get; set; }

        /// <summary>
        /// Current term number.
        /// </summary>
        public Int64State CurrentTermNumberField { get; set; }

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
        public Int64State AgeField { get; set; }

        /// <summary>
        /// DPoS information of each round.
        /// round number -> round information
        /// </summary>
        public MappedState<Int64Value, Round> RoundsMap { get; set; }

        /// <summary>
        /// DPoS mining interval.
        /// </summary>
        public Int32State MiningIntervalField { get; set; }

        /// <summary>
        /// Miners of each term.
        /// term number -> miners
        /// </summary>
        public MappedState<Int64Value, Miners> MinersMap { get; set; }

        /// <summary>
        /// Tickets of each address (public key).
        /// public key hex value -> tickets information
        /// </summary>
        public MappedState<StringValue, Tickets> TicketsMap { get; set; }

        /// <summary>
        /// Snapshots of all terms.
        /// term number -> snapshot
        /// </summary>
        public MappedState<Int64Value, TermSnapshot> SnapshotMap { get; set; }

        /// <summary>
        /// Aliases of candidates.
        /// candidate public key hex value -> alias
        /// </summary>
        public MappedState<StringValue, StringValue> AliasesMap { get; set; }

        /// <summary>
        /// Aliases of candidates.
        /// alias -> candidate public key hex value
        /// </summary>
        public MappedState<StringValue, StringValue> AliasesLookupMap { get; set; }

        /// <summary>
        /// Histories of all candidates
        /// candidate public key hex value -> history information
        /// </summary>
        public MappedState<StringValue, CandidateInHistory> HistoryMap { get; set; }

        /// <summary>
        /// blockchain age -> first round number.
        /// </summary>
        public MappedState<Int64Value, Int64Value> AgeToRoundNumberMap { get; set; }

        /// <summary>
        /// Keep tracking of the count of votes.
        /// </summary>
        public Int64State VotesCountField { get; set; }

        /// <summary>
        /// Keep tracking of the count of tickets.
        /// </summary>
        public Int64State TicketsCountField { get; set; }

        /// <summary>
        /// Whether 2/3 of miners mined in current term.
        /// </summary>
        public BoolState TwoThirdsMinersMinedCurrentTermField { get; set; }

        /// <summary>
        /// Transaction Id -> Voting Record.
        /// </summary>
        public MappedState<Hash, VotingRecord> VotingRecordsMap { get; set; }

        /// <summary>
        /// Term Number -> First Round Number of this term.
        /// </summary>
        public MappedState<Int64Value, Int64Value> TermToFirstRoundMap { get; set; }

        public Int64State TermNumberFromMainChainField { get; set; }
    }
}