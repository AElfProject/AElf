using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContractState : ContractState
    {
        public BoolState Initialized { get; set; }

        public SingletonState<int> TimeEachTerm { get; set; }

        public SingletonState<long> CurrentRoundNumber { get; set; }

        public SingletonState<long> CurrentTermNumber { get; set; }

        public SingletonState<Timestamp> BlockchainStartTimestamp { get; set; }

        public MappedState<long, Round> Rounds { get; set; }
        
        public SingletonState<int> MiningInterval { get; set; }

        public MappedState<long, long> FirstRoundNumberOfEachTerm { get; set; }

        public MappedState<long, Miners> MinersMap { get; set; }
        
        // TODO: Remove
        public SingletonState<int> BaseTimeUnit { get; set; }

        public SingletonState<long> MainChainRoundNumber { get; set; }

        public SingletonState<Miners> MainChainCurrentMiners { get; set; }

        public SingletonState<bool> IsMainChain { get; set; }
    }
}