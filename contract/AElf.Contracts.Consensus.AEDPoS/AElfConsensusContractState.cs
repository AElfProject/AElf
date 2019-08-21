using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContractState : ContractState
    {
        public BoolState Initialized { get; set; }

        /// <summary>
        /// Seconds.
        /// </summary>
        public Int64State TimeEachTerm { get; set; }

        public Int64State CurrentRoundNumber { get; set; }

        public Int64State CurrentTermNumber { get; set; }

        public SingletonState<Timestamp> BlockchainStartTimestamp { get; set; }

        public MappedState<long, Round> Rounds { get; set; }

        public Int32State MiningInterval { get; set; }

        public MappedState<long, long> FirstRoundNumberOfEachTerm { get; set; }

        public MappedState<long, MinerList> MinerListMap { get; set; }

        public Int64State MainChainRoundNumber { get; set; }

        public SingletonState<MinerList> MainChainCurrentMinerList { get; set; }

        public BoolState IsMainChain { get; set; }

        public Int64State MinerIncreaseInterval { get; set; }

        public MappedState<Hash, RandomNumberRequestInformation> RandomNumberInformationMap { get; set; }

        /// <summary>
        /// Round Number -> Random Number Token Hashes.
        /// Used for deleting due token hashes.
        /// </summary>
        public MappedState<long, HashList> RandomNumberTokenMap { get; set; }

        public Int64State LastIrreversibleBlockHeight { get; set; }

        public Int32State MaximumMinersCount { get; set; }

        public SingletonState<LatestProviderToTinyBlocksCount> LatestProviderToTinyBlocksCount { get; set; }
    }
}