using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types;
using Shouldly;
using Xunit;
using CustomContract = AElf.Runtime.CSharp.Tests.TestContract;

namespace AElf.Sdk.CSharp.Tests
{
    public class CSharpSmartContractContextTests : SdkCSharpTestBase
    {
        [Fact]
        public void Verify_Transaction_Origin_SetValue()
        {
            var bridgeContext = GetRequiredService<IHostSmartContractBridgeContextService>().Create();
            var origin = SampleAddress.AddressList[0];
            bridgeContext.TransactionContext = new TransactionContext
            {
                Origin = origin,
                Transaction = new Transaction()
                {
                    From = SampleAddress.AddressList[1],
                    To = SampleAddress.AddressList[2]
                }
            };
            var contractContext = new CSharpSmartContractContext(bridgeContext);
            contractContext.Origin.ShouldBe(origin); 
            contractContext.Origin.ShouldNotBe(bridgeContext.TransactionContext.Transaction.From);
        }
    }
}