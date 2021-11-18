using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.QuadraticFunding
{
    public partial class QuadraticFundingContractState : ContractState
    {
        public SingletonState<Address> Owner { get; set; }

        public StringState VoteSymbol { get; set; }

        /// <summary>
        /// * 1/10000
        /// </summary>
        public Int64State TaxPoint { get; set; }

        public Int64State Tax { get; set; }
        public Int64State CurrentRound { get; set; }
        public Int64State BasicVotingUnit { get; set; }

        /// <summary>
        /// Seconds.
        /// </summary>
        public Int64State Interval { get; set; }

        public MappedState<long, Timestamp> StartTimeMap { get; set; }
        public MappedState<long, Timestamp> EndTimeMap { get; set; }
        public MappedState<long, long> VotingUnitMap { get; set; }

        /// <summary>
        /// Project Id -> Project
        /// </summary>
        public MappedState<long, Project> ProjectMap { get; set; }

        /// <summary>
        /// Round Id -> Project List
        /// </summary>
        public MappedState<long, ProjectList> ProjectListMap { get; set; }

        public MappedState<long, long> SupportPoolMap { get; set; }

        public MappedState<long, long> PreTaxSupportPoolMap { get; set; }

        public MappedState<long, long> TotalSupportAreaMap { get; set; }

        public MappedState<long, bool> BanMap { get; set; }

        /// <summary>
        /// Project Id -> Voter -> Voted Amount
        /// </summary>
        public MappedState<long, Address, long> VotedMap { get; set; }
    }
}