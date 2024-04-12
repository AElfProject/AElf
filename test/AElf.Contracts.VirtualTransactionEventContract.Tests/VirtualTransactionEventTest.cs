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
        result.TransactionResult.Logs.Count.ShouldBe(1);
        var log = VirtualTransactionCreated.Parser.ParseFrom(result.TransactionResult.Logs
            .Where(e => e.Name.Contains(nameof(VirtualTransactionCreated))).Select(e => e.Indexed[2]).First());
        log.To.ShouldBe(AContractAddress);
        log = VirtualTransactionCreated.Parser.ParseFrom(result.TransactionResult.Logs
            .Where(e => e.Name.Contains(nameof(VirtualTransactionCreated))).Select(e => e.Indexed[3]).First());
        log.MethodName.ShouldBe("ExecuteAA");
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