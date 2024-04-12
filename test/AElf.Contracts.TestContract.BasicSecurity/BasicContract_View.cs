using System.Collections.Generic;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.BasicSecurity;

public partial class BasicSecurityContract
{
    public override StringValue GetContractName(Empty input)
    {
        return new StringValue
        {
            Value = nameof(BasicSecurityContract)
        };
    }

    public override BoolOutput QueryBoolState(Empty input)
    {
        return new BoolOutput
        {
            BoolValue = State.BoolInfo.Value
        };
    }

    public override BytesOutput QueryBytesState(Empty input)
    {
        return new BytesOutput
        {
            BytesValue = ByteString.CopyFrom(State.BytesInfo.Value)
        };
    }

    public override BytesOutput QueryBytesSingletonState(Empty input)
    {
        return new BytesOutput
        {
            BytesValue = ByteString.CopyFrom(State.BytesSingletonState.Value)
        };
    }

    public override Int32Output QueryInt32SingletonState(Empty input)
    {
        return new Int32Output
        {
            Int32Value = State.Int32SingletonState.Value
        };
    }

    public override Int32Value QueryEnumState(Empty input)
    {
        return new Int32Value
        {
            Value = (int)State.EnumState.Value
        };
    }

    public override Int32Output QueryInt32State(Empty input)
    {
        return new Int32Output
        {
            Int32Value = State.Int32Info.Value
        };
    }

    public override Int64Output QueryInt64State(Empty input)
    {
        return new Int64Output
        {
            Int64Value = State.Int64Info.Value
        };
    }

    public override StringOutput QueryStringState(Empty input)
    {
        return new StringOutput
        {
            StringValue = State.StringInfo.Value
        };
    }

    public override Complex1Output QueryComplex1State(Empty input)
    {
        return new Complex1Output
        {
            BoolValue = State.BoolInfo.Value,
            Int32Value = State.Int32Info.Value
        };
    }

    public override Complex2Output QueryComplex2State(Empty input)
    {
        return new Complex2Output
        {
            BoolData = new BoolOutput
            {
                BoolValue = State.BoolInfo.Value
            },
            Int32Data = new Int32Output
            {
                Int32Value = State.Int32Info.Value
            }
        };
    }

    public override ProtobufOutput QueryProtobufState(Empty input)
    {
        return new ProtobufOutput
        {
            ProtobufValue = State.ProtoInfo2.Value
        };
    }

    public override UInt32Output QueryUInt32State(Empty input)
    {
        return new UInt32Output
        {
            UInt32Value = State.UInt32Info.Value
        };
    }

    public override UInt64Output QueryUInt64State(Empty input)
    {
        return new UInt64Output
        {
            UInt64Value = State.UInt64Info.Value
        };
    }

    public override ProtobufMessage QueryMappedState(ProtobufInput input)
    {
        var message = State.MappedState[input.ProtobufValue.Int64Value];
        return message ?? new ProtobufMessage();
    }

    public override ProtobufMessage QueryMappedState1(ProtobufInput input)
    {
        var result = State.Complex3Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue];
        return result ?? new ProtobufMessage();
    }

    public override ProtobufMessage QueryMappedState2(ProtobufInput input)
    {
        var message = State.Complex4Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue][
            input.ProtobufValue.StringValue];

        return message ?? new ProtobufMessage();
    }

    public override TradeMessage QueryMappedState3(Complex3Input input)
    {
        var tradeMessage = State.Complex5Info[input.From][input.PairA][input.To][input.PairB];
        return tradeMessage ?? new TradeMessage();
    }

    public override Int64Output QueryExternalMethod1(Address input)
    {
        var data = State.BasicFunctionContract.QueryUserWinMoney.Call(input);

        return new Int64Output
        {
            Int64Value = data.Int64Value
        };
    }

    public override Int64Output QueryExternalMethod2(Address input)
    {
        var data = State.BasicFunctionContract.QueryUserLoseMoney.Call(input);

        return new Int64Output
        {
            Int64Value = data.Int64Value
        };
    }

    public override ResetOutput QueryFields(Empty input)
    {
        return new ResetOutput
        {
            // Int32Value = _number,
            Int64Value = _field1,
            StringValue = _field2 ?? string.Empty,
            BoolValue = _field3,
            List = { _list ?? new List<int>() }
        };
    }

    public override ConstOutput QueryConst(Empty input)
    {
        return new ConstOutput
        {
            Int64Const = Number,
            StringConst = String
        };
    }

    public override ResetNestedOutput QueryNestedFields(Empty input)
    {
        _innerContractType = new InnerContractType();
        return new ResetNestedOutput
        {
            Int32Value = _innerContractType.CheckNumberValue(),
            // StringValue = _innerContractType.CheckStaticValue() ?? string.Empty
        };
    }

    public override BoolValue CheckNonContractTypesStaticFieldsReset(Empty input)
    {
        // var res = InnerContractType.CheckAllStaticFieldsReset()
        //           && BasicContractTestType.CheckAllStaticFieldsReset()
        //           && BasicContractTestType.InnerTestType.CheckInnerTypeStaticFieldsReset();
        // Static field not allowed in user code https://github.com/AElfProject/AElf/issues/3388
        var res = true;
        return new BoolValue { Value = res };
    }

    public override BoolValue CheckFieldsAlreadyReset(Empty input)
    {
        var res = _field1 == 0 && _field2 == null && _field3 == false && _basicTestType == null &&
                  _innerContractType == null && dict == null;
        return new BoolValue { Value = res };
    }
}