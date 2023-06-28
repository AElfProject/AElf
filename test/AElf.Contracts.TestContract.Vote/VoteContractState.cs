using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.Vote;

public class VoteContractState : ContractState
{
    public MappedState<Address, StringValue> State { get; set; }
}