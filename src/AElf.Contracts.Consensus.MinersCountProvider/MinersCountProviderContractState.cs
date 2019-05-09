using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.MinersCountProvider
{
    public partial class MinersCountProviderContractState : ContractState
    {
        public SingletonState<Hash> MinersCountVotingItemId { get; set; }

        public SingletonState<MinersCountMode> Mode { get; set; }

        public SingletonState<bool> Configured { get; set; }
        
        public BoolState Initialized { get; set; }

        public SingletonState<int> MinersCount { get; set; }

        public SingletonState<int> Step { get; set; }

        public SingletonState<Timestamp> BlockchainStartTimestamp { get; set; }
    }
}