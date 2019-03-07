using System;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS.SideChain
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable once ClassNeverInstantiated.Global
    public class DPoSContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        public DividendContractReferenceState DividendContract { get; set; }
        public TokenContractReferenceState TokenContract { get; set; }

        /// <summary>
        /// Updated by UpdateMainChainConsensus method.
        /// </summary>
        public SingletonState<Miners> CurrentMiners { get; set; }

        /// <summary>
        /// Current round number.
        /// </summary>
        public UInt64State CurrentRoundNumberField { get; set; }

        /// <summary>
        /// Timestamp for genesis of this blockchain.
        /// </summary>
        public SingletonState<Timestamp> BlockchainStartTimestamp { get; set; }
        
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
        /// Aliases of candidates.
        /// candidate public key hex value -> alias
        /// </summary>
        public MappedState<StringValue, StringValue> AliasesMap { get; set; }

        /// <summary>
        /// Aliases of candidates.
        /// alias -> candidate public key hex value
        /// </summary>
        public MappedState<StringValue, StringValue> AliasesLookupMap { get; set; }
    }
}