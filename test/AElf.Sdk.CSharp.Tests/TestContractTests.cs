using System.Collections.Generic;
using System.Linq;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;
using CustomContract = AElf.Runtime.CSharp.Tests.TestContract;

namespace AElf.Sdk.CSharp.Tests
{
    public class TestContractTests : SdkCSharpTestBase
    {
        private CustomContract.TestContract Contract = new CustomContract.TestContract();
        private IStateProvider StateProvider { get; }
        private IHostSmartContractBridgeContext BridgeContext { get; }

        public TestContractTests()
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
        public void TestBoolState()
        {
            var input = new CustomContract.BoolInput
            {
                BoolValue = true
            };
            
            var output = Contract.TestBoolState(input);
            output.BoolValue.ShouldBeTrue();

            input.BoolValue = false;
            output = Contract.TestBoolState(input);
            output.BoolValue.ShouldBeFalse();
        }

        [Fact]
        public void TestInt32State()
        {
            var input = new CustomContract.Int32Input
            {
                Int32Value = 36
            };
            var output = Contract.TestInt32State(input);
            output.Int32Value.ShouldBe(-36);

            input.Int32Value = -100;
            output = Contract.TestInt32State(input);
            output.Int32Value.ShouldBe(64);
        }

        [Fact]
        public void UInt32Output()
        {
            var input = new CustomContract.UInt32Input
            {
                UInt32Value = 24
            };
            var output = Contract.TestUInt32State(input);
            output.UInt32Value.ShouldBe(24u);

            input.UInt32Value = 100;
            output = Contract.TestUInt32State(input);
            output.UInt32Value.ShouldBe(124u);
        }

        [Fact]
        public void TestInt64State()
        {
            var input = new CustomContract.Int64Input
            {
                Int64Value = 36
            };
            var output = Contract.TestInt64State(input);
            output.Int64Value.ShouldBe(-36);

            input.Int64Value = -100;
            output = Contract.TestInt64State(input);
            output.Int64Value.ShouldBe(64);
        }

        [Fact]
        public void TestUInt64State()
        {
            var input = new CustomContract.UInt64Input
            {
                UInt64Value = 24
            };
            var output = Contract.TestUInt64State(input);
            output.UInt64Value.ShouldBe(24ul);

            input.UInt64Value = 100;
            output = Contract.TestUInt64State(input);
            output.UInt64Value.ShouldBe(124ul);
        }

        [Fact]
        public void TestStringState()
        {
            var input = new CustomContract.StringInput
            {
                StringValue = "hello"
            };
            var output = Contract.TestStringState(input);
            output.StringValue.ShouldBe("hello");

            input.StringValue = " elf";
            output = Contract.TestStringState(input);
            output.StringValue.ShouldBe("hello elf");
        }

        [Fact]
        public void TestBytesState()
        {
            var input = new CustomContract.BytesInput
            {
                BytesValue = SampleAddress.AddressList[0].ToByteString()
            };

            var output = Contract.TestBytesState(input);
            output.BytesValue.ShouldBe(input.BytesValue);
        }

        [Fact]
        public void TestProtobufState()
        {
            var input = new CustomContract.ProtobufInput
            {
                 ProtobufValue = new CustomContract.ProtobufMessage
                 {
                     BoolValue = true,
                     Int64Value = 128,
                     StringValue = "test"
                 }
            };
            
            var output = Contract.TestProtobufState(input);
            output.ProtobufValue.BoolValue.ShouldBeTrue();
            output.ProtobufValue.Int64Value.ShouldBe(128);
            output.ProtobufValue.StringValue.ShouldBe("test");
        }

        [Fact]
        public void TestComplex1State()
        {
            var input = new CustomContract.Complex1Input
            {
                BoolValue = true,
                Int32Value = 120
            };
            var output = Contract.TestComplex1State(input);
            output.BoolValue.ShouldBe(true);
            output.Int32Value.ShouldBe(120);
        }

        [Fact]
        public void TestComplex2State()
        {
            var input = new CustomContract.Complex2Input
            {
                BoolData = new CustomContract.BoolInput(){ BoolValue = true },
                Int32Data = new CustomContract.Int32Input() { Int32Value = 12 }
            };
            var output = Contract.TestComplex2State(input);
            output.BoolData.BoolValue.ShouldBeTrue();
            output.Int32Data.Int32Value.ShouldBe(12);
        }

        [Fact]
        public void TestMappedState()
        {
            var input = new CustomContract.ProtobufInput
            {
                ProtobufValue = new CustomContract.ProtobufMessage
                {
                    BoolValue = false,
                    Int64Value = 100,
                    StringValue = "test"
                }
            };

            var output = Contract.TestMappedState(input);
            output.Collection.Count.ShouldBe(1);
            output.Collection[0].BoolValue.ShouldBeFalse();            
            output.Collection[0].Int64Value.ShouldBe(100);
            output.Collection[0].StringValue.ShouldBe("test");
        }
        
        [Fact]
        public void TestMapped1State()
        {
            var input = new CustomContract.Complex3Input
            {
                From = "A",
                PairA = "USD",
                To = "B",
                PairB = "RMB",
                TradeDetails = new CustomContract.TradeMessage
                {
                    FromAmount = 100,
                    ToAmount = 620
                }
            };
            
            var tradeMessage = Contract.TestMapped1State(input);
            tradeMessage.FromAmount.ShouldBe(100);
            tradeMessage.ToAmount.ShouldBe(620);
            
            tradeMessage = Contract.TestMapped1State(input);
            tradeMessage.FromAmount.ShouldBe(200);
            tradeMessage.ToAmount.ShouldBe(1240);

            input = new CustomContract.Complex3Input
            {
                From = "A",
                PairA = "EUR",
                To = "B",
                PairB = "RMB",
                TradeDetails = new CustomContract.TradeMessage
                {
                    FromAmount = 100,
                    ToAmount = 758
                }
            };
            tradeMessage = Contract.TestMapped1State(input);
            tradeMessage.FromAmount.ShouldBe(100);
            tradeMessage.ToAmount.ShouldBe(758);
        }
        
        [Fact]
        public void SendVirtualInline_Test()
        {
            BridgeContext.SendVirtualInline(Hash.FromString("hash"), SampleAddress.AddressList[0], "TestMethod", new CustomContract.StringInput
            {
                StringValue = "test send virtual inline"
            });
        }

        [Fact]
        public void SendInline_Test()
        {
            BridgeContext.SendInline(SampleAddress.AddressList[0], "TestMethod", new CustomContract.StringInput
            {
                StringValue = "test send inline"
            });
        }
    }
}