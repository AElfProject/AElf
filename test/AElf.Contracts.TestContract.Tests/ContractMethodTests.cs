using System.Threading.Tasks;
using AElf.Contracts.TestContract;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Contracts.TestContract.BasicSecurity;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using StringInput = AElf.Contracts.TestContract.BasicSecurity.StringInput;

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
            var transactionResult = (await TestBasicFunctionContractStub.UpdateBetLimit.SendAsync(
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
            var managerStub = GetTestBasicFunctionContractStub(SampleECKeyPairs.KeyPairs[1]);
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
            var managerStub = GetTestBasicFunctionContractStub(SampleECKeyPairs.KeyPairs[1]);
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
                var testUser = SampleECKeyPairs.KeyPairs[i];
                var basicStub = GetTestBasicFunctionContractStub(testUser);
                await basicStub.UserPlayBet.SendAsync(new BetInput
                {
                    Int64Value = 100
                });
            }

            var winMoney = (await TestBasicFunctionContractStub.QueryWinMoney.CallAsync(
                new Empty())).Int64Value;
            winMoney.ShouldBe(1000);

            var rewardMoney = (await TestBasicFunctionContractStub.QueryRewardMoney.CallAsync(
                new Empty())).Int64Value;
            rewardMoney.ShouldBeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task BasicContract_ValidateOrigin_Success()
        {
            var transaction1 = await TestBasicSecurityContractStub.TestOriginAddress.SendAsync(DefaultSender);
            transaction1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var transaction2 = await TestBasicFunctionContractStub.ValidateOrigin.SendAsync(DefaultSender);
            transaction2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        #endregion 
        
        #region BasicSecurity methods Test

        [Fact]
        public async Task BasicSecurity_BoolType_Test()
        {
            await TestBasicSecurityContractStub.TestBoolState.SendAsync(new BoolInput
            {
                BoolValue = false
            });

            var queryResult = (await TestBasicSecurityContractStub.QueryBoolState.CallAsync(new Empty()
            )).BoolValue;
            
            queryResult.ShouldBeFalse();
        }

        [Fact]
        public async Task BasicSecurity_Int32Type_Test()
        {
            await TestBasicSecurityContractStub.TestInt32State.SendAsync(new Int32Input
            {
                Int32Value = 30
            });

            var queryResult = (await TestBasicSecurityContractStub.QueryInt32State.CallAsync(new Empty()
            )).Int32Value;
            
            queryResult.ShouldBe(30);
        }

        [Fact]
        public async Task BasicSecurity_UInt32Type_Test()
        {
            await TestBasicSecurityContractStub.TestUInt32State.SendAsync(new UInt32Input
            {
                UInt32Value = 45
            });

            var queryResult = (await TestBasicSecurityContractStub.QueryUInt32State.CallAsync(new Empty()
            )).UInt32Value;
            
            queryResult.ShouldBe(45U);
        }
        
        [Fact]
        public async Task BasicSecurity_Int64Type_Test()
        {
            await TestBasicSecurityContractStub.TestInt64State.SendAsync(new Int64Input
            {
                Int64Value = 45
            });

            var queryResult = (await TestBasicSecurityContractStub.QueryInt64State.CallAsync(new Empty()
            )).Int64Value;
            
            queryResult.ShouldBe(45L);
        }
        
        [Fact]
        public async Task BasicSecurity_UInt64Type_Test()
        {
            await TestBasicSecurityContractStub.TestUInt64State.SendAsync(new UInt64Input
            {
                UInt64Value = 45
            });

            var queryResult = (await TestBasicSecurityContractStub.QueryUInt64State.CallAsync(new Empty()
            )).UInt64Value;
            
            queryResult.ShouldBe(45UL);
        }

        [Fact]
        public async Task BasicSecurity_StringType_Test()
        {
            await TestBasicSecurityContractStub.TestStringState.SendAsync(new StringInput
            {
                StringValue = "TestContract"
            });

            var queryResult = (await TestBasicSecurityContractStub.QueryStringState.CallAsync(new Empty()
            )).StringValue;
            queryResult.ShouldBe("TestContract");
        }
        
        [Fact]
        public async Task BasicSecurity_BytesType_Test()
        {
            await TestBasicSecurityContractStub.TestBytesState.SendAsync(new BytesInput
            {
                BytesValue = ByteString.CopyFromUtf8("test")
            });

            var queryResult = (await TestBasicSecurityContractStub.QueryBytesState.CallAsync(new Empty()
            )).BytesValue;
            queryResult.ShouldBe(ByteString.CopyFromUtf8("test"));
        }

        [Fact]
        public async Task BasicSecurity_ProtobufType_Test()
        {
            await TestBasicSecurityContractStub.TestProtobufState.SendAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    BoolValue = false,
                    Int64Value = 100L,
                    StringValue = "proto buf"
                }
            });

            var queryResult = (await TestBasicSecurityContractStub.QueryProtobufState.CallAsync(new Empty()
            )).ProtobufValue;
            queryResult.BoolValue.ShouldBeFalse();
            queryResult.Int64Value.ShouldBe(100L);
            queryResult.StringValue.ShouldBe("proto buf");
        }
        
        [Fact]
        public async Task BasicSecurity_Complex1Type_Test()
        {
            await TestBasicSecurityContractStub.TestComplex1State.SendAsync(new Complex1Input
                {
                    BoolValue = true,
                    Int32Value = 80
                });

            var queryResult = await TestBasicSecurityContractStub.QueryComplex1State.CallAsync(new Empty());
            queryResult.BoolValue.ShouldBeTrue();
            queryResult.Int32Value.ShouldBe(80);
        }

        [Fact]
        public async Task BasicSecurity_Complex2Type_Test()
        {
            await TestBasicSecurityContractStub.TestComplex2State.SendAsync(new Complex2Input
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

            var queryResult = await TestBasicSecurityContractStub.QueryComplex2State.CallAsync(new Empty());
            queryResult.BoolData.BoolValue.ShouldBeTrue();
            queryResult.Int32Data.Int32Value.ShouldBe(80);
        }

        [Fact]
        public async Task Basic_MappedType_Test()
        {
            await TestBasicSecurityContractStub.TestMapped1State.SendAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    StringValue = "test1",
                    Int64Value = 100
                }
            });

            //query check
            var queryResult = await TestBasicSecurityContractStub.QueryMappedState1.CallAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    StringValue = "test0",
                    Int64Value = 100
                }
            });
            queryResult.Int64Value.ShouldBe(0);
            
            var queryResult1 = await TestBasicSecurityContractStub.QueryMappedState1.CallAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    StringValue = "test1",
                    Int64Value = 100
                }
            });
            queryResult1.Int64Value.ShouldBe(100);
            
            await TestBasicSecurityContractStub.TestMapped1State.SendAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    StringValue = "test1",
                    Int64Value = 100
                }
            });
            
            queryResult1 = await TestBasicSecurityContractStub.QueryMappedState1.CallAsync(new ProtobufInput
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
            var from = SampleAddress.AddressList[0].GetFormatted();
            var pairA = "ELF";
            var to = SampleAddress.AddressList[1].GetFormatted();
            var pairB = "USDT";
                    
            await TestBasicSecurityContractStub.TestMapped2State.SendAsync(new Complex3Input
            {
                From = from,
                PairA = pairA,
                To = to,
                PairB = pairB,
                TradeDetails = new TradeMessage
                {
                    FromAmount = 1830,
                    ToAmount = 1000,
                    Timestamp = TimestampHelper.GetUtcNow()
                }
            });

            var queryResult = (await TestBasicSecurityContractStub.QueryMappedState2.CallAsync(new Complex3Input
            {
                From = from,
                PairA = pairA,
                To = to,
                PairB = pairB
            }));
            queryResult.FromAmount.ShouldBe(1830);
            queryResult.ToAmount.ShouldBe(1000);
            
            queryResult = (await TestBasicSecurityContractStub.QueryMappedState2.CallAsync(new Complex3Input
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