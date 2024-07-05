using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.PoA;

public class PoAContractState : ContractState
{
    public SingletonState<Address> Miner { get; set; }
    public Int64State MiningInterval { get; set; }
    public SingletonState<Timestamp> LastMiningTime { get; set; }
}