using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.Contracts
{
    // ReSharper disable InconsistentNaming
    public class DataCollection
    {
        /// <summary>
        /// Current round number.
        /// </summary>
        public UInt64Field CurrentRoundNumberField;
        
        /// <summary>
        /// Current term number.
        /// </summary>
        public UInt64Field CurrentTermNumberField;

        public PbField<Timestamp> BlockchainStartTimestamp;

        /// <summary>
        /// The nodes declared join the election for Miners.
        /// </summary>
        public PbField<Candidates> CandidatesField;

        /// <summary>
        /// Days since we started this blockchain.
        /// </summary>
        public UInt64Field AgeField;
        
        /// <summary>
        /// DPoS information of each round.
        /// </summary>
        public Map<UInt64Value, Round> RoundsMap;
        
        /// <summary>
        /// DPoS mining interval.
        /// </summary>
        public Int32Field MiningIntervalField;

        /// <summary>
        /// Round Number -> Term Number
        /// </summary>
        public Map<UInt64Value, UInt64Value> TermKeyLookUpMap;

        /// <summary>
        /// Tickets of each address (public key).
        /// </summary>
        public Map<StringValue, Tickets> TicketsMap;

        public Map<UInt64Value, TermSnapshot> SnapshotField;

        public Map<UInt64Value, UInt64Value> DividendsMap;

        public Map<StringValue, StringValue> AliasesMap;

        public Map<StringValue, CandidateInHistory> HistoryMap;
        
    }
}