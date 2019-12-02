using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using Shouldly;
using CustomContract = AElf.Runtime.CSharp.Tests.BadContract;

namespace AElf.Sdk.CSharp.Tests
{
    public class BadContractTests : SdkCSharpTestBase
    {
        private CustomContract.BadContract Contract = new CustomContract.BadContract();
        private IStateProvider StateProvider { get; }
        private IHostSmartContractBridgeContext BridgeContext { get; }

        public BadContractTests()
        {
            StateProvider = GetRequiredService<IStateProviderFactory>().CreateStateProvider();
            BridgeContext = GetRequiredService<IHostSmartContractBridgeContextService>().Create();

            var transactionContext = new TransactionContext()
            {
                Transaction = new Transaction()
                {
                    From = SampleAddress.AddressList[0],
                    To = SampleAddress.AddressList[1]
                }
            };

            BridgeContext.TransactionContext = transactionContext;

            Contract.InternalInitialize(BridgeContext);
        }
        
        [Fact]
        public void TestMaliciousCases()
        {
            Should.Throw<RuntimeBranchingThresholdExceededException>(() => Contract.TestInfiniteLoop(new Empty()));
            Should.Throw<RuntimeBranchingThresholdExceededException>(() => Contract.TestInfiniteLoopInSeparateClass(new Empty()));
        }
    }
}