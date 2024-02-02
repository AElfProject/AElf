using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.VirtualTransactionEvent;

public partial class VirtualTransactionEventTestContract : VirtualTransactionEventContractContainer.VirtualTransactionEventContractBase
{
    public override Empty FireVirtualTransactionEventTest(FireVirtualTransactionEventTestInput input)
    {
        var virtualHash = HashHelper.ComputeFrom("test");
        Context.SendVirtualInline(virtualHash,input.To,input.MethodName,input.Args,true);
        var virtualHash1 = HashHelper.ComputeFrom("virtual");
        Context.SendVirtualInline(virtualHash1,input.To,input.MethodName,input.Args,false);
        var virtualHash2 = HashHelper.ComputeFrom("virtualBlocked");
        Context.SendVirtualInline(virtualHash2, input.To, input.MethodName, input.Args, true, true);
        return new Empty();
    }

    public override Empty SendVirtualTransactionWithOutEvent(FireVirtualTransactionEventTestInput input)
    {
        var virtualHash = HashHelper.ComputeFrom("test1");
        Context.SendVirtualInline(virtualHash,input.To,input.MethodName,input.Args);
        return new Empty();
    }
}