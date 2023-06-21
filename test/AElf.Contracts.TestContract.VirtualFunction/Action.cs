using AElf.Contracts.Election;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.VirtualFunction;

public class Action : VirtualFunctionContractContainer.VirtualFunctionContractBase
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
    
    public override Empty VirtualAddressWithdraw(Hash input)
    {
        Initialize();

        Context.SendVirtualInline(HashHelper.ComputeFrom("test"), State.ElectionContract.Value, "Withdraw", input);

        return new Empty();
    }

    public override Address GetVirtualAddress(Empty input)
    {
        return Context.ConvertVirtualAddressToContractAddress(HashHelper.ComputeFrom("test"));
    }

    private void Initialize()
    {
        if (State.ElectionContract.Value == null)
        {
            State.ElectionContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName);
        }
    }
}