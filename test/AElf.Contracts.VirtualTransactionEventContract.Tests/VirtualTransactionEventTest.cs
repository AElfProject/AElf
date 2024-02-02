using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.TestContract.VirtualTransactionEvent;

public class VirtualTransactionEventTest : VirtualTransactionEventContractsTestBase
{
    [Fact]
    public async Task VirtualTransactionEvent_Test()
    {
        var result = await VirtualTransactionEventContractStub.FireVirtualTransactionEventTest.SendAsync(
            new FireVirtualTransactionEventTestInput
            {
                To = AContractAddress,
                MethodName = "ExecuteAA",
                Args = new StringValue
                {
                    Value = "Test"
                }.ToByteString()
            });
        result.TransactionResult.Logs.Count.ShouldBe(2);
        var virtualTransactionBlockedLog =
            result.TransactionResult.Logs.First(log => log.Name.Equals(nameof(VirtualTransactionBlocked)));

        VirtualTransactionBlocked virtualTransactionBlocked = new VirtualTransactionBlocked();
        for (int i = 0; i < virtualTransactionBlockedLog.Indexed.Count; i++)
        {
            virtualTransactionBlocked.MergeFrom(virtualTransactionBlockedLog.Indexed[i]);
        }

        virtualTransactionBlocked.To.ShouldBe(AContractAddress);
        virtualTransactionBlocked.MethodName.ShouldBe("ExecuteAA");

        var virtualTransactionCreatedLog =
            result.TransactionResult.Logs.First(log => log.Name.Equals(nameof(VirtualTransactionCreated)));

        var virtualTransactionCreated = new VirtualTransactionCreated();
        for (int i = 0; i < virtualTransactionCreatedLog.Indexed.Count; i++)
        {
            virtualTransactionCreated.MergeFrom(virtualTransactionCreatedLog.Indexed[i]);
        }

        virtualTransactionCreated.To.ShouldBe(AContractAddress);
        virtualTransactionCreated.MethodName.ShouldBe("ExecuteAA");
    }
    
    [Fact]
    public async Task VirtualTransactionWithOutEvent_Test()
    {
        var result = await VirtualTransactionEventContractStub.SendVirtualTransactionWithOutEvent.SendAsync(
            new FireVirtualTransactionEventTestInput
            {
                To = AContractAddress,
                MethodName = "ExecuteAA",
                Args = new StringValue
                {
                    Value = "Test"
                }.ToByteString()
            });
        result.TransactionResult.Logs.Count.ShouldBe(0);
    }

   
}