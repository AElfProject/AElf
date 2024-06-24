using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS;

public class AEDPoSContractState : ContractState
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
    public ReadonlyState<int> SingleNodeMiningInterval { get; set; }

    public MappedState<long, long> FirstRoundNumberOfEachTerm { get; set; }

    public MappedState<long, MinerList> MinerListMap { get; set; }

    public SingletonState<MinerList> MainChainCurrentMinerList { get; set; }

    public BoolState IsMainChain { get; set; }

    public ReadonlyState<long> MinerIncreaseInterval { get; set; }

    public Int32State MaximumMinersCount { get; set; }

    public SingletonState<LatestPubkeyToTinyBlocksCount> LatestPubkeyToTinyBlocksCount { get; set; }

    public MappedState<long, MinerList> MinedMinerListMap { get; set; }

    public SingletonState<Round> RoundBeforeLatestExecution { get; set; }

    public MappedState<long, Hash> RandomHashes { get; set; }

    public SingletonState<long> LatestExecutedHeight { get; set; }

    public BoolState IsPreviousBlockInSevereStatus { get; set; }
}