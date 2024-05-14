using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.Vote;

public class VoteContractState : ContractState
{
    public MappedState<string, string, string> State { get; set; }
}