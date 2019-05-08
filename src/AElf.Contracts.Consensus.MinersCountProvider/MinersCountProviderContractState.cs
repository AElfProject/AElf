using System;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Consensus.MinersCountProvider
{
    public partial class MinersCountProviderContractState : ContractState
    {
        public SingletonState<Hash> MinersCountVotingItemId { get; set; }

        public SingletonState<MinersCountMode> Mode { get; set; }
    }
}