using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Runtime.CSharp.Tests.TestContract;

public class TestContract : TestContractContainer.TestContractBase
{
    public override BoolOutput TestBoolState(BoolInput input)
    {
        State.BoolInfo.Value = input.BoolValue;
        return new BoolOutput
        {
            BoolValue = State.BoolInfo.Value
        };
    }

    public override Int32Output TestInt32State(Int32Input input)
    {
        State.Int32Info.Value = State.Int32Info.Value.Sub(input.Int32Value);
        return new Int32Output
        {
            Int32Value = State.Int32Info.Value
        };
    }

    public override UInt32Output TestUInt32State(UInt32Input input)
    {
        State.UInt32Info.Value = State.UInt32Info.Value.Add(input.UInt32Value);
        return new UInt32Output
        {
            UInt32Value = State.UInt32Info.Value
        };
    }

    public override Int64Output TestInt64State(Int64Input input)
    {
        State.Int64Info.Value = State.Int64Info.Value.Sub(input.Int64Value);
        return new Int64Output
        {
            Int64Value = State.Int64Info.Value
        };
    }

    public override UInt64Output TestUInt64State(UInt64Input input)
    {
        State.UInt64Info.Value = State.UInt64Info.Value.Add(input.UInt64Value);
        return new UInt64Output
        {
            UInt64Value = State.UInt64Info.Value
        };
    }

    public override StringOutput TestStringState(StringInput input)
    {
        if (string.IsNullOrEmpty(State.StringInfo.Value))
            State.StringInfo.Value = string.Empty;

        State.StringInfo.Value = State.StringInfo.Value + input.StringValue;
        return new StringOutput
        {
            StringValue = State.StringInfo.Value
        };
    }

    public override BytesOutput TestBytesState(BytesInput input)
    {
        State.BytesInfo.Value = input.BytesValue.ToByteArray();
        return new BytesOutput
        {
            BytesValue = ByteString.CopyFrom(State.BytesInfo.Value)
        };
    }

    public override ProtobufOutput TestProtobufState(ProtobufInput input)
    {
        State.ProtoInfo.Value = input.ProtobufValue;

        return new ProtobufOutput
        {
            ProtobufValue = State.ProtoInfo.Value
        };
    }

    public override Complex1Output TestComplex1State(Complex1Input input)
    {
        State.BoolInfo.Value = input.BoolValue;
        State.Int32Info.Value = input.Int32Value;

        return new Complex1Output
        {
            BoolValue = State.BoolInfo.Value,
            Int32Value = State.Int32Info.Value
        };
    }

    public override Complex2Output TestComplex2State(Complex2Input input)
    {
        State.BoolInfo.Value = input.BoolData.BoolValue;
        State.Int32Info.Value = input.Int32Data.Int32Value;

        return new Complex2Output
        {
            BoolData = new BoolOutput { BoolValue = State.BoolInfo.Value },
            Int32Data = new Int32Output { Int32Value = State.Int32Info.Value }
        };
    }

    public override ProtobufListOutput TestMappedState(ProtobufInput input)
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
            State.Complex3Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue].Int64Value =
                input.ProtobufValue.Int64Value;

        return new ProtobufListOutput
        {
            Collection = { input.ProtobufValue }
        };
    }

    public override TradeMessage TestMapped1State(Complex3Input input)
    {
        var tradeMessage = State.Complex4Info[input.From][input.PairA][input.To][input.PairB];
        if (tradeMessage == null)
        {
            State.Complex4Info[input.From][input.PairA][input.To][input.PairB] = input.TradeDetails;

            return input.TradeDetails;
        }

        tradeMessage.FromAmount += input.TradeDetails.FromAmount;
        tradeMessage.ToAmount += input.TradeDetails.ToAmount;

        State.Complex4Info[input.From][input.PairA][input.To][input.PairB] = tradeMessage;

        return tradeMessage;
    }

    public override BoolOutput TestReadonlyState(BoolInput input)
    {
        State.ReadonlyBool.Value = input.BoolValue;
        return new BoolOutput { BoolValue = State.ReadonlyBool.Value };
    }

    public override StringOutput TestArrayIterateForeach(Empty input)
    {
        // Iterating array via foreach loop causes unchecked arithmetic opcodes
        // This is to be used for contract policy tests

        var words = new[] { "TEST", "FOREACH", "LOOP" };
        var merged = "";

        foreach (var word in words) merged += $" {word}";

        return new StringOutput { StringValue = merged };
    }

    public override Int32Output TestViewMethod(Empty input)
    {
        return new Int32Output
        {
            Int32Value = State.Int32Info.Value + 1
        };
    }

    // for test cases
    public bool TestStateType(int i)
    {
        State.ReadonlyBool.Value = false;
        State.ProtoInfo.Value = new ProtobufMessage();
        State.Int32Info.Value = int.MaxValue;
        State.MappedState[1] = new Address();
        State.MappedInt64State[1] = i + 1;
        State.StringInfo.Value = "test";
        return State.BoolInfo.Value;
    }

    public class TestNestClass
    {

        public void TestState()
        {/* State update is not allowed in non-contract class
            var state = new TestContractState();
            state.ProtoInfo.Value = new ProtobufMessage();*/
        }
    }
}