using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.Vote;

public class VoteContract : VoteContractContainer.VoteContractBase
{
    public override Empty AddOption(AddOptionInput input)
    {
        return new Empty();
    }
}