using System;
using System.Threading.Tasks;
using AElf.Contracts.TestContract;
using AElf.Contracts.TestContract.Basic2;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using StringInput = AElf.Contracts.TestContract.Basic2.StringInput;

namespace AElf.Contract.TestContract
{
    public class ContractMethodTests : TestContractTestBase
    {
        public ContractMethodTests()
        {
            InitializeTestContracts();
        }

        #region Basic1 methods Test
        [Fact]
        public async Task Basic1Contract_UpdateBetLimit_WithoutPermission()
        {
            var transactionResult = (await TestBasic1ContractStub.UpdateBetLimit.SendAsync(
                new BetLimitInput
                {
                    MinValue = 50,
                    MaxValue = 100
                })).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Only manager can perform this action").ShouldBeTrue();
        }
        
        [Fact]
        public async Task Basic1Contract_UpdateBetLimit_WithException()
        {
            var managerStub = GetTestBasic1ContractStub(SampleECKeyPairs.KeyPairs[1]);
            var transactionResult = (await managerStub.UpdateBetLimit.SendAsync(
                new BetLimitInput
                {
                    MinValue = 100,
                    MaxValue = 50
                })).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Invalid min/max value input setting").ShouldBeTrue();
        }
        
        [Fact]
        public async Task Basic1Contract_UpdateBetLimit_Success()
        {
            var managerStub = GetTestBasic1ContractStub(SampleECKeyPairs.KeyPairs[1]);
            var transactionResult = (await managerStub.UpdateBetLimit.SendAsync(
                new BetLimitInput
                {
                    MinValue = 100,
                    MaxValue = 200
                })).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task Basic1Contract_QueryMethod()
        {
            for (int i = 0; i < 10; i++)
            {
                await TestBasic1ContractStub.UserPlayBet.SendAsync(new BetInput
                {
                    Int64Value = 100
                });
            }

            var winMoney = (await TestBasic1ContractStub.QueryWinMoney.CallAsync(
                new Empty())).Int64Value;
            winMoney.ShouldBe(1000);

            var rewardMoney = (await TestBasic1ContractStub.QueryRewardMoney.CallAsync(
                new Empty())).Int64Value;
            rewardMoney.ShouldBeGreaterThanOrEqualTo(0);
        }
        
        #endregion 
        
        #region Basic2 methods Test

        [Fact]
        public async Task Basic2_BoolType_Test()
        {
            await TestBasic2ContractStub.TestBoolState.SendAsync(new BoolInput
            {
                BoolValue = false
            });

            var queryResult = (await TestBasic2ContractStub.QueryBoolState.CallAsync(new Empty()
            )).BoolValue;
            
            queryResult.ShouldBeFalse();
        }

        [Fact]
        public async Task Basic2_Int32Type_Test()
        {
            await TestBasic2ContractStub.TestInt32State.SendAsync(new Int32Input
            {
                Int32Value = 30
            });

            var queryResult = (await TestBasic2ContractStub.QueryInt32State.CallAsync(new Empty()
            )).Int32Value;
            
            queryResult.ShouldBe(30);
        }

        [Fact]
        public async Task Basic2_UInt32Type_Test()
        {
            await TestBasic2ContractStub.TestUInt32State.SendAsync(new UInt32Input
            {
                UInt32Value = 45
            });

            var queryResult = (await TestBasic2ContractStub.QueryUInt32State.CallAsync(new Empty()
            )).UInt32Value;
            
            queryResult.ShouldBe(45U);
        }
        
        [Fact]
        public async Task Basic2_Int64Type_Test()
        {
            await TestBasic2ContractStub.TestInt64State.SendAsync(new Int64Input
            {
                Int64Value = 45
            });

            var queryResult = (await TestBasic2ContractStub.QueryInt64State.CallAsync(new Empty()
            )).Int64Value;
            
            queryResult.ShouldBe(45L);
        }
        
        [Fact]
        public async Task Basic2_UInt64Type_Test()
        {
            await TestBasic2ContractStub.TestUInt64State.SendAsync(new UInt64Input
            {
                UInt64Value = 45
            });

            var queryResult = (await TestBasic2ContractStub.QueryUInt64State.CallAsync(new Empty()
            )).UInt64Value;
            
            queryResult.ShouldBe(45UL);
        }

        [Fact]
        public async Task Basic2_StringType_Test()
        {
            await TestBasic2ContractStub.TestStringState.SendAsync(new StringInput
            {
                StringValue = "TestContract"
            });

            var queryResult = (await TestBasic2ContractStub.QueryStringState.CallAsync(new Empty()
            )).StringValue;
            queryResult.ShouldBe("TestContract");
        }
        
        [Fact]
        public async Task Basic2_BytesType_Test()
        {
            await TestBasic2ContractStub.TestBytesState.SendAsync(new BytesInput
            {
                BytesValue = ByteString.CopyFromUtf8("test")
            });

            var queryResult = (await TestBasic2ContractStub.QueryBytesState.CallAsync(new Empty()
            )).BytesValue;
            queryResult.ShouldBe(ByteString.CopyFromUtf8("test"));
        }

        [Fact]
        public async Task Basic2_ProtobufType_Test()
        {
            await TestBasic2ContractStub.TestProtobufState.SendAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    BoolValue = false,
                    Int64Value = 100L,
                    StringValue = "proto buf"
                }
            });

            var queryResult = (await TestBasic2ContractStub.QueryProtobufState.CallAsync(new Empty()
            )).ProtobufValue;
            queryResult.BoolValue.ShouldBeFalse();
            queryResult.Int64Value.ShouldBe(100L);
            queryResult.StringValue.ShouldBe("proto buf");
        }
        
        [Fact]
        public async Task Basic2_Complex1Type_Test()
        {
            await TestBasic2ContractStub.TestComplex1State.SendAsync(new Complex1Input
                {
                    BoolValue = true,
                    Int32Value = 80
                });

            var queryResult = await TestBasic2ContractStub.QueryComplex1State.CallAsync(new Empty());
            queryResult.BoolValue.ShouldBeTrue();
            queryResult.Int32Value.ShouldBe(80);
        }

        [Fact]
        public async Task Basic2_Complex2Type_Test()
        {
            await TestBasic2ContractStub.TestComplex2State.SendAsync(new Complex2Input
            {
                BoolData = new BoolInput
                {
                    BoolValue = true
                },
                Int32Data = new Int32Input
                {
                    Int32Value = 80
                }
            });

            var queryResult = await TestBasic2ContractStub.QueryComplex2State.CallAsync(new Empty());
            queryResult.BoolData.BoolValue.ShouldBeTrue();
            queryResult.Int32Data.Int32Value.ShouldBe(80);
        }

        [Fact]
        public async Task Basic_MappedType_Test()
        {
            await TestBasic2ContractStub.TestMapped1State.SendAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    StringValue = "test1",
                    Int64Value = 100
                }
            });

            //query check
            var queryResult = await TestBasic2ContractStub.QueryMappedState1.CallAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    StringValue = "test0",
                    Int64Value = 100
                }
            });
            queryResult.Int64Value.ShouldBe(0);
            
            var queryResult1 = await TestBasic2ContractStub.QueryMappedState1.CallAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    StringValue = "test1",
                    Int64Value = 100
                }
            });
            queryResult1.Int64Value.ShouldBe(100);
            
            await TestBasic2ContractStub.TestMapped1State.SendAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    StringValue = "test1",
                    Int64Value = 100
                }
            });
            
            queryResult1 = await TestBasic2ContractStub.QueryMappedState1.CallAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    StringValue = "test1",
                    Int64Value = 100
                }
            });
            queryResult1.Int64Value.ShouldBe(200);
        }
        
        [Fact]
        public async Task Basic_Mapped1Type_Test()
        {
            var from = Address.Generate().GetFormatted();
            var pairA = "ELF";
            var to = Address.Generate().GetFormatted();
            var pairB = "USDT";
                    
            await TestBasic2ContractStub.TestMapped2State.SendAsync(new Complex3Input
            {
                From = from,
                PairA = pairA,
                To = to,
                PairB = pairB,
                TradeDetails = new TradeMessage
                {
                    FromAmount = 1830,
                    ToAmount = 1000,
                    Timestamp = DateTime.UtcNow.ToTimestamp()
                }
            });

            var queryResult = (await TestBasic2ContractStub.QueryMappedState2.CallAsync(new Complex3Input
            {
                From = from,
                PairA = pairA,
                To = to,
                PairB = pairB
            }));
            queryResult.FromAmount.ShouldBe(1830);
            queryResult.ToAmount.ShouldBe(1000);
            
            queryResult = (await TestBasic2ContractStub.QueryMappedState2.CallAsync(new Complex3Input
            {
                From = from,
                PairA = pairA,
                To = to,
                PairB = "ETH"
            }));
            queryResult.FromAmount.ShouldBe(0);
            queryResult.ToAmount.ShouldBe(0);
        }
        
        #endregion
    }
}