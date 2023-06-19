using AElf.Contracts.Election;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.TestVote;

public class Action : TestVoteContractContainer.TestVoteContractBase
{
    public override Empty VirtualAddressVote(VirtualAddressVoteInput input)
    {
        Initialize();

        Context.SendVirtualInline(HashHelper.ComputeFrom("test"), State.ElectionContract.Value, "Vote", new VoteMinerInput
        {
            CandidatePubkey = input.PubKey,
            Amount = input.Amount,
            EndTimestamp = input.EndTimestamp,
            Token = input.Token
        });

        return new Empty();
    }
    
    private void Initialize()
    {
        State.ElectionContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName);
    }
}