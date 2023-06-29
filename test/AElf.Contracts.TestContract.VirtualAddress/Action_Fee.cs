using AElf.Sdk.CSharp;
using AElf.Standards.ACS1;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.VirtualAddress;

public partial class Action
{
    [View]
    public override MethodFees GetMethodFee(StringValue input)
    {
        var methodFees = State.TransactionFees[input.Value];
        if (methodFees != null)
            return methodFees;

        //set default tx fee
        return new MethodFees
        {
            MethodName = input.Value,
            Fees =
            {
                new MethodFee { Symbol = "ELF", BasicFee = 1000_0000 }
            }
        };
    }

    public override Empty SetMethodFee(MethodFees input)
    {
        State.TransactionFees[input.MethodName] = input;
        return new Empty();
    }
}