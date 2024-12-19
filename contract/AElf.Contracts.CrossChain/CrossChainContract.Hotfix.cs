using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.CrossChain;

public partial class CrossChainContract
{
    public override Empty SetParentChainHeight(Int64Value input)
    {
        AssertAddressIsCurrentMiner(Context.Sender);
        State.CurrentParentChainHeight.Value = input.Value;
        return new Empty();
    }
}