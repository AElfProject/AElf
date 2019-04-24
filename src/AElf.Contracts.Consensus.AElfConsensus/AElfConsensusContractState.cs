using AElf.Consensus.AElfConsensus;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AElfConsensus
{
    public partial class AElfConsensusContractState : ContractState
    {
        public BoolState Initialized { get; set; }

        public SingletonState<long> LockTokenForElection { get; set; }

        public SingletonState<bool> IsTermChangeable { get; set; }

        public SingletonState<bool> IsSideChain { get; set; }

        public SingletonState<int> DaysEachTerm { get; set; }

        public SingletonState<long> CurrentRoundNumber { get; set; }

        public SingletonState<long> CurrentTermNumber { get; set; }

        public SingletonState<Timestamp> BlockchainStartTimestamp { get; set; }

        public MappedState<long, Round> Rounds { get; set; }
    }
}