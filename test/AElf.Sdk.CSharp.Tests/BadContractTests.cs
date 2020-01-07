using System;
using System.Linq;
using System.Reflection;
using AElf.CSharp.CodeOps;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types;
using Google.Protobuf;
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
            Should.Throw<RuntimeBranchThresholdExceededException>(() => Contract.TestInfiniteLoop(new Empty()));
            ClearObserver();
        }

        [Fact]
        public void TestInfiniteLoop_InSeparateClass()
        {
            SetObserver();
            Should.Throw<RuntimeBranchThresholdExceededException>(() => Contract.TestInfiniteLoopInSeparateClass(new Empty()));
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
        
        [Fact]
        public void TestGetHashCodeInContract_FromInput()
        {
            var str = "GetHashCode Test";

            var output = Contract.TestGetHashCodeFromInput(new CustomContract.GetHashCodeTestInput
            {
                BoolValue = true,
                Int32Value = Int32.MaxValue,
                UInt32Value = UInt32.MaxValue,
                Int64Value = Int64.MaxValue,
                UInt64Value = UInt64.MaxValue,
                StringValue = str,
                RepeatedStringValue = { str, str },
                EnumValue = CustomContract.Color.Black,
                BytesValue = ByteString.CopyFromUtf8(str)
            });
            
            output.BoolHash.ShouldBe(1);
            output.Int32Hash.ShouldBe(2147483647);
            output.Uint32Hash.ShouldBe(-1);
            output.Int64Hash.ShouldBe(-2147483648);
            output.Uint64Hash.ShouldBe(0);
            output.StringHash.ShouldBe(1417273651);
            output.BytesHash.ShouldBe(-347977704);
            output.RepeatedStringHash.ShouldBe(0);
            output.EnumHash.ShouldBe(0);
            output.InputHash.ShouldBe(-1086609109);
        }
        
        [Fact]
        public void TestGetHashCodeInContract_FromContract()
        {
            var output = Contract.TestGetHashCodeFromContract(new Empty());
            
            output.BoolHash.ShouldBe(1);
            output.Int32Hash.ShouldBe(2147483647);
            output.Uint32Hash.ShouldBe(-1);
            output.Int64Hash.ShouldBe(-2147483648);
            output.Uint64Hash.ShouldBe(0);
            output.StringHash.ShouldBe(1417273651);
            output.BytesHash.ShouldBe(0);
            output.RepeatedStringHash.ShouldBe(1);
            output.EnumHash.ShouldBe(0);
            output.InputHash.ShouldBe(1417273651); // For StringValue
        }
    }
}
