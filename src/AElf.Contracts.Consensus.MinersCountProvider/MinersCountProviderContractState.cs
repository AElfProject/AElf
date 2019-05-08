using System;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Consensus.MinersCountProvider
{
    public partial class MinersCountProviderContractState : ContractState
    {
        public SingletonState<Hash> MinersCountVotingItemId { get; set; }

        public SingletonState<MinersCountMode> Mode { get; set; }

        public SingletonState<bool> IsInitialMinersCountSet { get; set; }
        
        public BoolState Initialized { get; set; }

        public SingletonState<int> MinersCount { get; set; }
    }
}