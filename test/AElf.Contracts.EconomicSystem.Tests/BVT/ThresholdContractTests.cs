using System.Threading.Tasks;
using Acs5;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.EconomicSystem.Tests.BVT
{
    public class ThresholdContractTests : EconomicSystemTestBase
    {
        [Fact]
        public async Task MethodCallingThreshold_Test()
        {
            var methodResult = await MethodCallThresholdContractStub.GetMethodCallingThreshold.CallAsync(
                new StringValue
                {
                    Value = "SendForFun"
                });
            methodResult.SymbolToAmount.Count.ShouldBe(0);

            var setResult = await MethodCallThresholdContractStub.SetMethodCallingThreshold.SendAsync(
                new SetMethodCallingThresholdInput
                {
                    Method = "SendForFun",
                    SymbolToAmount =
                    {
                        {"ELF", 1_0000_0000}
                    },
                    ThresholdCheckType = ThresholdCheckType.Balance
                });
            setResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            methodResult = await MethodCallThresholdContractStub.GetMethodCallingThreshold.CallAsync(
                new StringValue
                {
                    Value = "SendForFun"
                });
            methodResult.SymbolToAmount.Count.ShouldBe(1);
            methodResult.ThresholdCheckType.ShouldBe(ThresholdCheckType.Balance);

            var executionResult = await MethodCallThresholdContractStub.SendForFun.SendAsync(new Empty());
            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }
}