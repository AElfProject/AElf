using Acs1;
using Acs10;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable UnusedAutoPropertyAccessor.Global
    public partial class AEDPoSContractState : ContractState
    {
        public BoolState Initialized { get; set; }

        /// <summary>
        /// Seconds.
        /// </summary>
        public ReadonlyState<long> PeriodSeconds { get; set; }

        public Int64State CurrentRoundNumber { get; set; }

        public Int64State CurrentTermNumber { get; set; }

        public ReadonlyState<Timestamp> BlockchainStartTimestamp { get; set; }

        public MappedState<long, Round> Rounds { get; set; }

        public ReadonlyState<int> MiningInterval { get; set; }

        public MappedState<long, long> FirstRoundNumberOfEachTerm { get; set; }

        public MappedState<long, MinerList> MinerListMap { get; set; }

        public Int64State MainChainRoundNumber { get; set; }

        public SingletonState<MinerList> MainChainCurrentMinerList { get; set; }

        public BoolState IsMainChain { get; set; }

        public ReadonlyState<long> MinerIncreaseInterval { get; set; }

        public MappedState<Hash, RandomNumberRequestInformation> RandomNumberInformationMap { get; set; }

        /// <summary>
        /// Round Number -> Random Number Token Hashes.
        /// Used for deleting due token hashes.
        /// </summary>
        public MappedState<long, HashList> RandomNumberTokenMap { get; set; }

        public Int32State MaximumMinersCount { get; set; }

        public SingletonState<LatestPubkeyToTinyBlocksCount> LatestPubkeyToTinyBlocksCount { get; set; }

        public MappedState<long, MinerList> MinedMinerListMap { get; set; }
        public MappedState<string, MethodFees> TransactionFees { get; set; }

        public SingletonState<Round> RoundBeforeLatestExecution { get; set; }
        public SingletonState<AuthorityInfo> MethodFeeController { get; set; }

        public MappedState<long, Hash> RandomHashes { get; set; }

        public SingletonState<long> LatestExecutedHeight { get; set; }

        public MappedState<long, Dividends> SideChainReceivedDividends { get; set; }

        public SingletonState<Hash> SideChainDividendPoolSchemeId { get; set; }
    }
}