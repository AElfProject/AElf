using AElf.Standards.ACS1;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.PoA;

public partial class PoAContract
{
    public override MethodFees GetMethodFee(StringValue input)
    {
        return new MethodFees();
    }
}