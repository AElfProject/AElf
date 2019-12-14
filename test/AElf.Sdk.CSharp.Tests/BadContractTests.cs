using System.Linq;
using AElf.CSharp.CodeOps;
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
            
            var injectedCounter =  Contract.GetType().Assembly
                    .GetTypes().SingleOrDefault(t => t.Name == nameof(ExecutionObserverProxy));
            injectedCounter.ShouldNotBeNull();
            
            var proxyCountMethod = injectedCounter.GetMethod(nameof(ExecutionObserverProxy.Initialize), new[] { typeof(IExecutionObserver) });
            proxyCountMethod.ShouldNotBeNull();
            
            // Initialize injected type since we don't use CSharpSmartContractProxy here
            proxyCountMethod.Invoke(null, new object[] {
                    BridgeContext.ExecutionObserver
            });
        }
        
        [Fact]
        public void TestInfiniteLoop_InContractImplementation()
        {
            Should.Throw<RuntimeBranchingThresholdExceededException>(() => Contract.TestInfiniteLoop(new Empty()));
        }

        [Fact]
        public void TestInfiniteLoop_InSeparateClass()
        {
            Should.Throw<RuntimeBranchingThresholdExceededException>(() => Contract.TestInfiniteLoopInSeparateClass(new Empty()));
        }
    }
}