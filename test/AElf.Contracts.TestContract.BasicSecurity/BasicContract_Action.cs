using System.Collections.Generic;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.BasicSecurity;

public partial class BasicSecurityContract : BasicSecurityContractContainer.BasicSecurityContractBase
{
    private const int Number = 1;
    private const string String = "TEST";
    // private static int _number;// NOTE: Static field not allowed in user code https://github.com/AElfProject/AElf/issues/3388

    // public static InnerContractType _innerContractTypeStaticField;
    private BasicContractTestType _basicTestType;
    private long _field1;
    private string _field2;
    private bool _field3;
    private InnerContractType _innerContractType;
    private List<int> _list;

    private Dictionary<long, long> dict { get; set; }

    public override Empty InitialBasicSecurityContract(Address input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");

        //set basic1 contract reference
        Assert(input != null, "Basic1Contract address is not exist.");
        State.BasicFunctionContract.Value = input;

        State.Initialized.Value = true;
        State.BoolInfo.Value = true;
        State.Int32Info.Value = 0;
        State.UInt32Info.Value = 0;
        State.Int64Info.Value = 0;
        State.UInt64Info.Value = 0;
        State.StringInfo.Value = string.Empty;
        State.BytesInfo.Value = new byte[] { };
        return new Empty();
    }

    public override Empty TestBoolState(BoolInput input)
    {
        State.BoolInfo.Value = input.BoolValue;

        return new Empty();
    }

    public override Empty TestInt32State(Int32Input input)
    {
        State.Int32Info.Value = State.Int32Info.Value.Add(input.Int32Value);

        return new Empty();
    }

    public override Empty TestUInt32State(UInt32Input input)
    {
        State.UInt32Info.Value = State.UInt32Info.Value.Add(input.UInt32Value);

        return new Empty();
    }

    public override Empty TestInt64State(Int64Input input)
    {
        State.Int64Info.Value = State.Int64Info.Value.Add(input.Int64Value);

        return new Empty();
    }

    public override Empty TestUInt64State(UInt64Input input)
    {
        State.UInt64Info.Value = State.UInt64Info.Value.Add(input.UInt64Value);

        return new Empty();
    }

    public override Empty TestStringState(StringInput input)
    {
        State.StringInfo.Value = input.StringValue;
        return new Empty();
    }

    public override Empty TestBytesState(BytesInput input)
    {
        State.BytesInfo.Value = input.BytesValue.ToByteArray();

        return new Empty();
    }

    public override Empty TestBytesSingletonState(BytesInput input)
    {
        State.BytesSingletonState.Value = input.BytesValue.ToByteArray();

        return new Empty();
    }

    public override Empty TestInt32SingletonState(Int32Input input)
    {
        State.Int32SingletonState.Value = input.Int32Value;
        return new Empty();
    }

    public override Empty TestEnumState(Int32Input input)
    {
        State.EnumState.Value = (StateEnum)input.Int32Value;
        return new Empty();
    }

    public override Empty TestProtobufState(ProtobufInput input)
    {
        State.ProtoInfo2.Value = input.ProtobufValue;

        return new Empty();
    }

    public override Empty TestSingletonState(ProtobufInput input)
    {
        State.ProtoInfo.Value = input.ProtobufValue;

        return new Empty();
    }

    public override Empty TestComplex1State(Complex1Input input)
    {
        State.BoolInfo.Value = input.BoolValue;
        State.Int32Info.Value = input.Int32Value;

        return new Empty();
    }

    public override Empty TestComplex2State(Complex2Input input)
    {
        State.BoolInfo.Value = input.BoolData.BoolValue;
        State.Int32Info.Value = input.Int32Data.Int32Value;

        return new Empty();
    }

    public override Empty TestMappedState(ProtobufInput input)
    {
        var protobufMessage = State.MappedState[input.ProtobufValue.Int64Value];
        if (protobufMessage == null)
            State.MappedState[input.ProtobufValue.Int64Value] =
                new ProtobufMessage
                {
                    BoolValue = true,
                    Int64Value = input.ProtobufValue.Int64Value,
                    StringValue = input.ProtobufValue.StringValue
                };
        else
            State.MappedState[input.ProtobufValue.Int64Value] =
                new ProtobufMessage
                {
                    BoolValue = true,
                    Int64Value = State.Complex3Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue]
                        .Int64Value
                        .Add(input.ProtobufValue.Int64Value),
                    StringValue = input.ProtobufValue.StringValue
                };

        return new Empty();
    }

    public override Empty TestMapped1State(ProtobufInput input)
    {
        var protobufMessage = State.Complex3Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue];
        if (protobufMessage == null)
            State.Complex3Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue] =
                new ProtobufMessage
                {
                    BoolValue = true,
                    Int64Value = input.ProtobufValue.Int64Value,
                    StringValue = input.ProtobufValue.StringValue
                };
        else
            State.Complex3Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue] =
                new ProtobufMessage
                {
                    BoolValue = true,
                    Int64Value = State.Complex3Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue]
                        .Int64Value
                        .Add(input.ProtobufValue.Int64Value),
                    StringValue = input.ProtobufValue.StringValue
                };

        return new Empty();
    }

    public override Empty TestMapped2State(ProtobufInput input)
    {
        var protobufMessage =
            State.Complex4Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue][
                input.ProtobufValue.StringValue];
        if (protobufMessage == null)
            State.Complex4Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue][
                    input.ProtobufValue.StringValue] =
                new ProtobufMessage
                {
                    BoolValue = true,
                    Int64Value = input.ProtobufValue.Int64Value,
                    StringValue = input.ProtobufValue.StringValue
                };
        else
            State.Complex4Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue][
                    input.ProtobufValue.StringValue] =
                new ProtobufMessage
                {
                    BoolValue = true,
                    Int64Value = State.Complex3Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue]
                        .Int64Value
                        .Add(input.ProtobufValue.Int64Value),
                    StringValue = input.ProtobufValue.StringValue
                };

        return new Empty();
    }

    public override Empty TestMapped3State(Complex3Input input)
    {
        var tradeMessage = State.Complex5Info[input.From][input.PairA][input.To][input.PairB];
        if (tradeMessage == null)
        {
            input.TradeDetails.Timestamp = Context.CurrentBlockTime;
            State.Complex5Info[input.From][input.PairA][input.To][input.PairB] = input.TradeDetails;
        }
        else
        {
            tradeMessage.FromAmount = tradeMessage.FromAmount.Add(input.TradeDetails.FromAmount);
            tradeMessage.ToAmount = tradeMessage.ToAmount.Add(input.TradeDetails.ToAmount);
            tradeMessage.Timestamp = Context.CurrentBlockTime;

            State.Complex5Info[input.From][input.PairA][input.To][input.PairB] = tradeMessage;
        }

        return new Empty();
    }

    //Reference call action
    public override Empty TestExecuteExternalMethod(Int64Input input)
    {
        var feeValue = input.Int64Value.Mul(5).Div(100);
        var betValue = input.Int64Value.Sub(feeValue);

        State.Int64Info.Value.Add(feeValue);
        State.BasicFunctionContract.UserPlayBet.Send(new BetInput
        {
            Int64Value = betValue
        });

        return new Empty();
    }

    public override Empty TestOriginAddress(Address address)
    {
        State.BasicFunctionContract.ValidateOrigin.Send(address);
        return new Empty();
    }

    public override Empty TestResetFields(ResetInput input)
    {
        // _number = input.Int32Value;
        _field1 = input.Int64Value;
        _field2 = input.StringValue;
        _field3 = input.BoolValue;
        State.Int64Info.Value = Number;
        State.StringInfo.Value = String;
        dict = new Dictionary<long, long>();
        _list = new List<int> { 1 };
        return new Empty();
    }

    public override ResetNestedOutput TestResetNestedFields(ResetNestedInput input)
    {
        _innerContractType = new InnerContractType();
        _innerContractType.SetValue(input.Int32Value, input.StringValue);
        var number = _innerContractType.CheckNumberValue();
        // var staticString = _innerContractType.CheckStaticValue();

        return new ResetNestedOutput
        {
            Int32Value = number,
            // StringValue = staticString
        };
    }

    public override ResetOtherTypeNestedOutput TestResetOtherTypeFields(ResetNestedInput input)
    {
        _basicTestType = new BasicContractTestType();
        _basicTestType.SetBasicContractTestType(input.Int32Value);
        var s = _basicTestType.CheckFunc().Invoke(input.StringValue);
        // var testType = _basicTestType.CheckTypeValue();
        // _basicTestType.SetStaticField();
        // BasicContractTestType.BasicContractTestTypePublicStaticField = new BasicContractTestType();

        var innerTestTypeObj = new BasicContractTestType.InnerTestType(1);
        innerTestTypeObj.SetStaticField();
        // BasicContractTestType.InnerTestType.InnerTestTypePublicStaticField =
        //     new BasicContractTestType.InnerTestType(2);

        // var innerContractTypeObj = new InnerContractType();
        // innerContractTypeObj.SetStaticField();
        // InnerContractType.InnerContractTypePublicStaticField = new InnerContractType();

        return new ResetOtherTypeNestedOutput
        {
            // TypeConst = testType.CheckConstNumberValue(),
            // TypeNumber = testType.CheckNumberValue(),
            BasicTypeNumber = _basicTestType.CheckNumberValue(),
            // BasicTypeStaticNumber = _basicTestType.CheckStaticNumberValue(),
            StringValue = s
        };
    }

    public override Int32Output TestWhileInfiniteLoop(Int32Input input)
    {
        var i = 0;
        var count = input.Int32Value;
        while (i < count) i++;

        return new Int32Output { Int32Value = i };
    }

    public override Int32Output TestWhileInfiniteLoopWithState(Int32Input input)
    {
        var i = 0;
        var count = input.Int32Value;
        while (i++ < count)
            if (i % 7 == 0)
                State.LoopInt32Value.Value = i;

        return new Int32Output { Int32Value = State.LoopInt32Value.Value };
    }

    public override Int32Output TestForInfiniteLoop(Int32Input input)
    {
        var i = 0;
        var count = input.Int32Value;
        for (i = 0; i < count; i++)
        {
        }

        return new Int32Output { Int32Value = i };
    }

    public override Int32Output TestForInfiniteLoopInSeparateClass(Int32Input input)
    {
        SeparateClass.UseInfiniteLoopInSeparateClass(input.Int32Value);
        return new Int32Output();
    }

    public override Int32Output TestForeachInfiniteLoop(ListInput input)
    {
        var i = 1;
        foreach (var t in input.List) i++;

        return new Int32Output { Int32Value = i };
    }

    public override Empty TestInfiniteRecursiveCall(Int32Input input)
    {
        RecursiveCall(input.Int32Value);
        return new Empty();
    }

    public override Empty TestInfiniteRecursiveCallInSeparateClass(Int32Input input)
    {
        SeparateClass.UseInfiniteRecursiveCallInSeparateClass(input.Int32Value);
        return new Empty();
    }

    private void RecursiveCall(int value)
    {
        if (value > 0)
            RecursiveCall(value - 1);
    }

    public class InnerContractType
    {
        // private static string _testTypeString; // Static field not allowed in user code https://github.com/AElfProject/AElf/issues/3388
        //
        // private static InnerContractType _innerContractTypePrivateStaticField; // Static field not allowed in user code https://github.com/AElfProject/AElf/issues/3388
        // public static InnerContractType InnerContractTypePublicStaticField; // Static field not allowed in user code https://github.com/AElfProject/AElf/issues/3388
        private int _testTypeNumber;

        public void SetValue(int number, string s)
        {
            _testTypeNumber = number;
            // _testTypeString = s;
        }

        public int CheckNumberValue()
        {
            return _testTypeNumber;
        }

        // Static field not allowed in user code https://github.com/AElfProject/AElf/issues/3388
        // public string CheckStaticValue()
        // {
        //     return _testTypeString;
        // }

        // Static field not allowed in user code https://github.com/AElfProject/AElf/issues/3388
        // public void SetStaticField()
        // {
        //     _innerContractTypePrivateStaticField = this;
        // }

        // public static bool CheckAllStaticFieldsReset()
        // {
        //     return _testTypeString == null && _innerContractTypePrivateStaticField == null &&
        //            InnerContractTypePublicStaticField == null;
        // }
    }
}

internal class SeparateClass
{
    public static void UseInfiniteLoopInSeparateClass(int count)
    {
        for (var i = 0; i < count;) i++;
    }

    public static void UseInfiniteRecursiveCallInSeparateClass(int count)
    {
        if (count <= 0)
            return;
        UseInfiniteRecursiveCallInSeparateClass(count - 1);
    }
}