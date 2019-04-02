using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    public partial class DPoSContractState
    {
        /// <summary>
        /// Histories of all candidates
        /// candidate public key hex value -> history information
        /// </summary>
        public MappedState<StringValue, CandidateInHistory> HistoryMap { get; set; }

        /// <summary>
        /// Keep tracking of the count of votes.
        /// </summary>
        public Int64State VotesCountField { get; set; }

        /// <summary>
        /// Keep tracking of the count of tickets.
        /// </summary>
        public Int64State TicketsCountField { get; set; }

        /// <summary>
        /// Transaction Id -> Voting Record.
        /// </summary>
        public MappedState<Hash, VotingRecord> VotingRecordsMap { get; set; }
        
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
    }
}