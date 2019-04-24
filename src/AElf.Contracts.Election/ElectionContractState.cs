using AElf.Kernel;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        public BoolState TreasuryCreated { get; set; }
        public BoolState TreasuryRegistered { get; set; }
        public BoolState VotingEventRegistered { get; set; }
        public SingletonState<Hash> TreasuryHash { get; set; }
        public SingletonState<Hash> WelfareHash { get; set; }
        public SingletonState<Hash> SubsidyHash { get; set; }
        public SingletonState<Hash> RewardHash { get; set; }
        public SingletonState<Hash> BasicRewardHash { get; set; }
        public SingletonState<Hash> VotesWeightRewardHash { get; set; }
        public SingletonState<Hash> ReElectionRewardHash { get; set; }

        public MappedState<string, Votes> Votes { get; set; }

        public MappedState<string, CandidateHistory> Histories { get; set; }

        public SingletonState<int> CurrentTermNumber { get; set; }

        /// <summary>
        /// Vote Id -> Lock Time (days)
        /// </summary>
        public MappedState<Hash, int> LockTimeMap { get; set; }


    }
}