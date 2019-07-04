using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContractState : ContractState
    {
        public BoolState Initialized { get; set; }

        /// <summary>
        /// Seconds.
        /// </summary>
        public SingletonState<long> TimeEachTerm { get; set; }

        public SingletonState<long> CurrentRoundNumber { get; set; }

        public SingletonState<long> CurrentTermNumber { get; set; }

        public SingletonState<Timestamp> BlockchainStartTimestamp { get; set; }

        public MappedState<long, Round> Rounds { get; set; }
        
        public SingletonState<int> MiningInterval { get; set; }

        public MappedState<long, long> FirstRoundNumberOfEachTerm { get; set; }

        public MappedState<long, MinerList> MinerListMap { get; set; }
        
        public SingletonState<long> MainChainRoundNumber { get; set; }

        public SingletonState<MinerList> MainChainCurrentMinerList { get; set; }

        public SingletonState<bool> IsMainChain { get; set; }
        
        public SingletonState<long> MinerIncreaseInterval { get; set; }
    }
}