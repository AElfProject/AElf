using System;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
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
        /// Term Number -> First Round Number of this term.
        /// </summary>
        public MappedState<Int64Value, Int64Value> TermToFirstRoundMap { get; set; }
    }
}