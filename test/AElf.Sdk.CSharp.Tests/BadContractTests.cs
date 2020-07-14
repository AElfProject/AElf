using System.Linq;
using System.Reflection;
using AElf.CSharp.CodeOps;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
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

        private readonly MethodInfo _proxyCountMethod;

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
            
            _proxyCountMethod = injectedCounter.GetMethod(nameof(ExecutionObserverProxy.SetObserver), new[] { typeof(IExecutionObserver) });
            _proxyCountMethod.ShouldNotBeNull();
        }

        private void SetObserver()
        {
            // Initialize injected type since we don't execute the contract the same way as mining
            _proxyCountMethod.Invoke(null, new object[] {
                new ExecutionObserver(100, 100), 
            });
        }
        
        private void ClearObserver()
        {
            // Initialize injected type since we don't execute the contract the same way as mining
            _proxyCountMethod.Invoke(null, new object[] { null });
        }

        [Fact]
        public void TestInfiniteLoop_InContractImplementation()
        {
            SetObserver();
            Should.NotThrow(() =>
                Contract.TestInfiniteLoop(new Int32Value {Value = 100}));
            ClearObserver();
            SetObserver();
            Should.Throw<RuntimeBranchThresholdExceededException>(() =>
                Contract.TestInfiniteLoop(new Int32Value {Value = 101}));
            ClearObserver();
            SetObserver();
            Should.Throw<RuntimeBranchThresholdExceededException>(() =>
                Contract.TestInfiniteLoopInSeparateClass(new Empty()));
            ClearObserver();
        }
        
        [Fact]
        public void TestInfiniteRecursiveCall_InContractImplementation()
        {
            SetObserver();
            Should.Throw<RuntimeCallThresholdExceededException>(() => Contract.TestInfiniteRecursiveCall(new Empty()));
            ClearObserver();
        }

        [Fact]
        public void TestInfiniteRecursiveCall_InSeparateClass()
        {
            SetObserver();
            Should.Throw<RuntimeCallThresholdExceededException>(() => Contract.TestInfiniteRecursiveCallInSeparateClass(new Empty()));
            ClearObserver();
        }
    }
}