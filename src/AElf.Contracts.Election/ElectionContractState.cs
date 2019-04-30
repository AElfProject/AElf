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
        
        // TODO: We can merge some Ids.
        public SingletonState<Hash> TreasuryHash { get; set; }
        
        public SingletonState<Hash> WelfareHash { get; set; }
        public SingletonState<Hash> SubsidyHash { get; set; }
        public SingletonState<Hash> RewardHash { get; set; }
        
        public SingletonState<Hash> BasicRewardHash { get; set; }
        public SingletonState<Hash> VotesWeightRewardHash { get; set; }
        public SingletonState<Hash> ReElectionRewardHash { get; set; }

        // TODO: Divide and rename.
        public MappedState<string, Votes> Votes { get; set; }

        // TODO: Rename to Candidate(Information)
        public MappedState<string, CandidateHistory> Histories { get; set; }

        public SingletonState<int> CurrentTermNumber { get; set; }

        public SingletonState<PublicKeysList> Candidates { get; set; }

        public SingletonState<PublicKeysList> InitialMiners { get; set; }

        /// <summary>
        /// Vote Id -> Lock Time (days)
        /// </summary>
        public MappedState<Hash, int> LockTimeMap { get; set; }

        public MappedState<long, TermSnapshot> Snapshots { get; set; }

        public SingletonState<int> MinersCount { get; set; }

        public SingletonState<int> BaseTimeUnit { get; set; }

        public SingletonState<int> MinimumLockTime { get; set; }

        public SingletonState<int> MaximumLockTime { get; set; }

    }
}