using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.FakeVote;

public class VoteContractState : ContractState
{
    public MappedState<Address, StringValue> State { get; set; }
}