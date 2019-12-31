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
    public class GetHashCodeTest : SdkCSharpTestBase
    {
        private CustomContract.BadContract Contract = new CustomContract.BadContract();
        private IStateProvider StateProvider { get; }
        private IHostSmartContractBridgeContext BridgeContext { get; }

        private readonly MethodInfo _proxyCountMethod;

        // TODO: Merge with BadContractTests
        public GetHashCodeTest()
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
        public void TestGetHashCodeInContract()
        {
            var str = "GetHashCode Test";
            var output = Contract.TestGetHashCode(new CustomContract.GetHashCodeTestInput
            {
                BoolValue = true,
                Int32Value = Int32.MaxValue,
                UInt32Value = UInt32.MaxValue,
                Int64Value = Int64.MaxValue,
                UInt64Value = UInt64.MaxValue,
                StringValue = str,
                BytesValue = ByteString.CopyFromUtf8(str)
            });
            
            output.BoolHash.ShouldBe(1);
            output.Int32Hash.ShouldBe(2147483647);
            output.Uint32Hash.ShouldBe(-1);
            output.Int64Hash.ShouldBe(-2147483648);
            output.Uint64Hash.ShouldBe(0);
            output.StringHash.ShouldBe(100);
            output.BytesHash.ShouldBe(-347977704);
        }
    }
}