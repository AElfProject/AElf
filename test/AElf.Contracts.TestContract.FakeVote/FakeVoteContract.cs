using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.FakeVote;

public class FakeVoteContract : FakeVoteContractContainer.FakeVoteContractBase
{
    public override Empty AddOption(AddOptionInput input)
    {
        return new Empty();
    }
}