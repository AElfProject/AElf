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
            var origin = AddressHelper.FromString("origin");
            bridgeContext.TransactionContext = new TransactionContext
            {
                Origin = origin,
                Transaction = new Transaction()
                {
                    From = AddressHelper.FromString("from"),
                    To = AddressHelper.FromString("to")
                }
            };
            var contractContext = new CSharpSmartContractContext(bridgeContext);
            contractContext.Origin.ShouldBe(origin); 
            contractContext.Origin.ShouldNotBe(bridgeContext.TransactionContext.Transaction.From);
        }
    }
}