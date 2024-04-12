using System.Text;
using System.Threading.Tasks;
using AElf.Contracts.TestContract.BasicSecurity;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using SmartContractConstants = AElf.Kernel.SmartContract.SmartContractConstants;

namespace AElf.Contract.TestContract;

public class PatchedContractSecurityTests : TestContractTestBase
{
    public PatchedContractSecurityTests()
    {
        InitializePatchedContracts();
    }

    [Fact(Skip="Static field not allowed in user code https://github.com/AElfProject/AElf/issues/3388")]
    public async Task ResetFields_Test()
    {
        var result = await TestBasicSecurityContractStub.TestResetFields.SendAsync(new ResetInput
        {
            BoolValue = true,
            Int32Value = 100,
            Int64Value = 1000,
            StringValue = "TEST"
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var int64 = await TestBasicSecurityContractStub.QueryInt64State.CallAsync(new Empty());
        var s = await TestBasicSecurityContractStub.QueryStringState.CallAsync(new Empty());
        var constValue = await TestBasicSecurityContractStub.QueryConst.CallAsync(new Empty());
        int64.Int64Value.Equals(constValue.Int64Const).ShouldBeTrue();
        s.StringValue.Equals(constValue.StringConst).ShouldBeTrue();

        var fields = await TestBasicSecurityContractStub.QueryFields.CallAsync(new Empty());
        fields.BoolValue.ShouldBeFalse();
        fields.Int32Value.ShouldBe(0);
        fields.Int64Value.ShouldBe(0);
        fields.StringValue.ShouldBe(string.Empty);
        fields.List.ShouldBeEmpty();

        var allFieldReset = await TestBasicSecurityContractStub.CheckFieldsAlreadyReset.CallAsync(new Empty());
        allFieldReset.Value.ShouldBeTrue();
    }

    [Fact(Skip = "Static field not allowed in user code https://github.com/AElfProject/AElf/issues/3388")]
    public async Task Reset_NestedFields_Test()
    {
        var result = await TestBasicSecurityContractStub.TestResetNestedFields.SendAsync(new ResetNestedInput
        {
            Int32Value = 100,
            StringValue = "TEST"
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        result.Output.Int32Value.ShouldBe(100);
        result.Output.StringValue.ShouldBe("TEST");
        var fields = await TestBasicSecurityContractStub.QueryNestedFields.CallAsync(new Empty());
        fields.Int32Value.ShouldBe(0);
        fields.StringValue.ShouldBe(string.Empty);

        var allFieldReset = await TestBasicSecurityContractStub.CheckFieldsAlreadyReset.CallAsync(new Empty());
        allFieldReset.Value.ShouldBeTrue();
    }

    [Fact(Skip = "Static field not allowed in user code https://github.com/AElfProject/AElf/issues/3388")]
    public async Task Reset_OtherType_NestedFields_Test()
    {
        var result = await TestBasicSecurityContractStub.TestResetOtherTypeFields.SendAsync(new ResetNestedInput
        {
            Int32Value = 100,
            StringValue = "TEST"
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        result.Output.StringValue.ShouldBe("test");
        result.Output.BasicTypeNumber.ShouldBe(100);
        result.Output.BasicTypeStaticNumber.ShouldBe(100);
        result.Output.TypeConst.ShouldBe(1);
        result.Output.TypeNumber.ShouldBe(100);


        var allFieldReset = await TestBasicSecurityContractStub.CheckFieldsAlreadyReset.CallAsync(new Empty());
        allFieldReset.Value.ShouldBeTrue();

        var allStaticFieldsReset =
            await TestBasicSecurityContractStub.CheckNonContractTypesStaticFieldsReset.CallAsync(new Empty());
        allStaticFieldsReset.Value.ShouldBeTrue();
    }

    [Fact]
    public async Task TestSingletonStateSizeLimit()
    {
        var stateSizeLimit = SmartContractConstants.StateSizeLimit;

        // bytes
        {
            var txResult = await TestBasicSecurityContractStub.TestBytesState.SendWithExceptionAsync(new BytesInput
            {
                BytesValue = ByteString.CopyFrom(new byte[stateSizeLimit])
            });

            txResult.TransactionResult.Error.ShouldContain($"exceeds limit of {stateSizeLimit}");
            await TestBasicSecurityContractStub.TestBytesState.SendAsync(new BytesInput
            {
                BytesValue = ByteString.CopyFrom(new byte[stateSizeLimit - 3])
            });

            var queryResult = await TestBasicSecurityContractStub.QueryBytesState.CallAsync(new Empty());
            queryResult.BytesValue.ShouldBe(new byte[stateSizeLimit - 3]);

            var txResult2 = await TestBasicSecurityContractStub.TestBytesState.SendWithExceptionAsync(new BytesInput
            {
                BytesValue = ByteString.CopyFrom(new byte[stateSizeLimit])
            });
            txResult2.TransactionResult.Error.ShouldContain($"exceeds limit of {stateSizeLimit}");
        }

        // string
        {
            var str = Encoding.UTF8.GetString(new byte[stateSizeLimit + 1]);

            var txResult = await TestBasicSecurityContractStub.TestStringState.SendWithExceptionAsync(
                new StringInput
                {
                    StringValue = str
                });
            txResult.TransactionResult.Error.ShouldContain($"exceeds limit of {stateSizeLimit}");

            var str1 = Encoding.UTF8.GetString(new byte[stateSizeLimit]);

            await TestBasicSecurityContractStub.TestStringState.SendAsync(new StringInput
            {
                StringValue = str1
            });

            var queryResult = await TestBasicSecurityContractStub.QueryStringState.CallAsync(new Empty());
            queryResult.StringValue.ShouldBe(str1);

            txResult = await TestBasicSecurityContractStub.TestStringState.SendWithExceptionAsync(new StringInput
            {
                StringValue = str
            });
            txResult.TransactionResult.Error.ShouldContain($"exceeds limit of {stateSizeLimit}");
        }

        // proto type
        {
            var str = Encoding.UTF8.GetString(new byte[stateSizeLimit]);
            var txResult = await TestBasicSecurityContractStub.TestProtobufState.SendWithExceptionAsync(
                new ProtobufInput
                {
                    ProtobufValue = new ProtobufMessage
                    {
                        StringValue = str
                    }
                });
            txResult.TransactionResult.Error.ShouldContain($"exceeds limit of {stateSizeLimit}");

            var str1 = Encoding.UTF8.GetString(new byte[stateSizeLimit - 10]);

            await TestBasicSecurityContractStub.TestProtobufState.SendAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    StringValue = str1
                }
            });

            var queryResult = await TestBasicSecurityContractStub.QueryProtobufState.CallAsync(new Empty());
            queryResult.ProtobufValue.ShouldBe(new ProtobufMessage
            {
                StringValue = str1
            });

            txResult = await TestBasicSecurityContractStub.TestProtobufState.SendWithExceptionAsync(
                new ProtobufInput
                {
                    ProtobufValue = new ProtobufMessage
                    {
                        StringValue = str
                    }
                });
            txResult.TransactionResult.Error.ShouldContain($"exceeds limit of {stateSizeLimit}");
        }

        {
            await TestBasicSecurityContractStub.TestInt32SingletonState.SendAsync(new Int32Input
            {
                Int32Value = int.MaxValue
            });

            var queryResult = await TestBasicSecurityContractStub.QueryInt32SingletonState.CallAsync(new Empty());
            queryResult.Int32Value.ShouldBe(int.MaxValue);
        }

        // enum
        {
            await TestBasicSecurityContractStub.TestEnumState.SendAsync(new Int32Input
            {
                Int32Value = (int)StateEnum.Foo
            });

            var queryResult = await TestBasicSecurityContractStub.QueryEnumState.CallAsync(new Empty());
            queryResult.Value.ShouldBe((int)StateEnum.Foo);
        }
    }

    [Fact]
    public async Task TestMappedStateSizeLimit()
    {
        var stateSizeLimit = SmartContractConstants.StateSizeLimit;

        {
            var str = Encoding.UTF8.GetString(new byte[stateSizeLimit]);
            var txResult = await TestBasicSecurityContractStub.TestMappedState.SendWithExceptionAsync(
                new ProtobufInput
                {
                    ProtobufValue = new ProtobufMessage
                    {
                        StringValue = str
                    }
                });
            txResult.TransactionResult.Error.ShouldContain($"exceeds limit of {stateSizeLimit}");

            await TestBasicSecurityContractStub.TestMappedState.SendAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    Int64Value = 1
                }
            });

            var queryResult = await TestBasicSecurityContractStub.QueryMappedState.CallAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    Int64Value = 1
                }
            });

            queryResult.Int64Value.ShouldBe(1);

            (await TestBasicSecurityContractStub.QueryMappedState.CallAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    Int64Value = 2
                }
            })).ShouldBe(new ProtobufMessage());
        }

        {
            var str = Encoding.UTF8.GetString(new byte[stateSizeLimit]);
            var txResult = await TestBasicSecurityContractStub.TestMapped1State.SendWithExceptionAsync(
                new ProtobufInput
                {
                    ProtobufValue = new ProtobufMessage
                    {
                        StringValue = str
                    }
                });
            txResult.TransactionResult.Error.ShouldContain($"exceeds limit of {stateSizeLimit}");

            var str1 = Encoding.UTF8.GetString(new byte[10]);
            var message = new ProtobufMessage
            {
                BoolValue = true,
                Int64Value = 1,
                StringValue = str1
            };
            await TestBasicSecurityContractStub.TestMapped1State.SendAsync(new ProtobufInput
            {
                ProtobufValue = message
            });

            var queryResult = await TestBasicSecurityContractStub.QueryMappedState1.CallAsync(new ProtobufInput
            {
                ProtobufValue = message
            });

            queryResult.ShouldBe(message);

            (await TestBasicSecurityContractStub.QueryMappedState1.CallAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    BoolValue = true,
                    Int64Value = 2,
                    StringValue = str1
                }
            })).ShouldBe(new ProtobufMessage());
        }

        {
            var str = Encoding.UTF8.GetString(new byte[stateSizeLimit]);
            var txResult = await TestBasicSecurityContractStub.TestMapped2State.SendWithExceptionAsync(
                new ProtobufInput
                {
                    ProtobufValue = new ProtobufMessage
                    {
                        StringValue = str
                    }
                });

            await TestBasicSecurityContractStub.TestMapped2State.SendAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage()
            });
            txResult.TransactionResult.Error.ShouldContain($"exceeds limit of {stateSizeLimit}");

            var str1 = Encoding.UTF8.GetString(new byte[10]);
            var message = new ProtobufMessage
            {
                BoolValue = true,
                Int64Value = 1,
                StringValue = str1
            };

            await TestBasicSecurityContractStub.TestMapped2State.SendAsync(new ProtobufInput
            {
                ProtobufValue = message
            });

            var queryResult = await TestBasicSecurityContractStub.QueryMappedState2.CallAsync(new ProtobufInput
            {
                ProtobufValue = message
            });

            queryResult.ShouldBe(message);

            (await TestBasicSecurityContractStub.QueryMappedState2.CallAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    BoolValue = true,
                    Int64Value = 2,
                    StringValue = str1
                }
            })).ShouldBe(new ProtobufMessage());
        }

        {
            var str = Encoding.UTF8.GetString(new byte[stateSizeLimit]);
            var message = new TradeMessage
            {
                FromAmount = 1024
            };

            var txResult = await TestBasicSecurityContractStub.TestMapped3State.SendWithExceptionAsync(
                new Complex3Input
                {
                    TradeDetails = new TradeMessage
                    {
                        FromAmount = 1,
                        Memo = str
                    }
                });

            txResult.TransactionResult.Error.ShouldContain($"exceeds limit of {stateSizeLimit}");

            var str1 = Encoding.UTF8.GetString(new byte[10]);

            var complex3Input = new Complex3Input
            {
                From = str1,
                To = str1,
                TradeDetails = message
            };
            await TestBasicSecurityContractStub.TestMapped3State.SendAsync(complex3Input);

            var queryResult = await TestBasicSecurityContractStub.QueryMappedState3.CallAsync(complex3Input);

            queryResult.FromAmount.ShouldBe(message.FromAmount);

            (await TestBasicSecurityContractStub.QueryMappedState3.CallAsync(new Complex3Input
            {
                From = str1,
                To = str1,
                PairA = str1,
                TradeDetails = message
            })).ShouldBe(new TradeMessage());
        }
    }

    [Fact]
    public async Task TestBranchCount()
    {
        {
            await TestBasicSecurityContractStub.TestWhileInfiniteLoop.SendAsync(new Int32Input
                { Int32Value = 14999 });
            var txResult = await TestBasicSecurityContractStub.TestWhileInfiniteLoop.SendWithExceptionAsync(
                new Int32Input
                    { Int32Value = 15000 });
            txResult.TransactionResult.Error.ShouldContain(nameof(RuntimeBranchThresholdExceededException));
        }

        {
            await TestBasicSecurityContractStub.TestForInfiniteLoop.SendAsync(new Int32Input { Int32Value = 14999 });
            var txResult = await TestBasicSecurityContractStub.TestForInfiniteLoop.SendWithExceptionAsync(
                new Int32Input
                    { Int32Value = 15000 });
            txResult.TransactionResult.Error.ShouldContain(nameof(RuntimeBranchThresholdExceededException));
        }

        {
            await TestBasicSecurityContractStub.TestForInfiniteLoopInSeparateClass.SendAsync(new Int32Input
                { Int32Value = 14999 });
            var txResult = await TestBasicSecurityContractStub.TestForInfiniteLoop.SendWithExceptionAsync(
                new Int32Input
                    { Int32Value = 15000 });
            txResult.TransactionResult.Error.ShouldContain(nameof(RuntimeBranchThresholdExceededException));
        }

        {
            await TestBasicSecurityContractStub.TestWhileInfiniteLoopWithState.SendAsync(new Int32Input
                { Int32Value = 14999 });
            var txResult =
                await TestBasicSecurityContractStub.TestWhileInfiniteLoopWithState.SendWithExceptionAsync(
                    new Int32Input
                        { Int32Value = 15000 });
            txResult.TransactionResult.Error.ShouldContain(nameof(RuntimeBranchThresholdExceededException));
        }

        {
            await TestBasicSecurityContractStub.TestForeachInfiniteLoop.SendAsync(new ListInput
                { List = { new int[14999] } });
            var txResult =
                await TestBasicSecurityContractStub.TestForeachInfiniteLoop.SendWithExceptionAsync(
                    new ListInput { List = { new int[15000] } });
            txResult.TransactionResult.Error.ShouldContain(nameof(RuntimeBranchThresholdExceededException));
        }
    }

    [Fact]
    public async Task TestMethodCallCount()
    {
        {
            await TestBasicSecurityContractStub.TestInfiniteRecursiveCall.SendAsync(new Int32Input
                { Int32Value = 14900 });
            var txResult = await TestBasicSecurityContractStub.TestInfiniteRecursiveCall.SendWithExceptionAsync(
                new Int32Input { Int32Value = 15000 });
            txResult.TransactionResult.Error.ShouldContain(nameof(RuntimeCallThresholdExceededException));
        }

        {
            await TestBasicSecurityContractStub.TestInfiniteRecursiveCallInSeparateClass.SendAsync(new Int32Input
                { Int32Value = 14900 });
            var txResult = await TestBasicSecurityContractStub.TestInfiniteRecursiveCall.SendWithExceptionAsync(
                new Int32Input { Int32Value = 15000 });
            txResult.TransactionResult.Error.ShouldContain(nameof(RuntimeCallThresholdExceededException));
        }
    }
}