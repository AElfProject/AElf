using AElf.Contracts.TestContract.BasicFunction;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.BasicSecurity
{
    public partial class BasicSecurityContract : BasicSecurityContractContainer.BasicSecurityContractBase
    {
        private const int Number = 1;
        private const string String = "TEST";
        private static int _number;
        private long _field1;
        private string _field2;
        private bool _field3;
        private BasicContractTestType _basicTestType;
        private InnerContractType _innerContractType;

        public static InnerContractType _innerContractTypeStaticField;

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
            if (string.IsNullOrEmpty(State.StringInfo.Value))
                State.StringInfo.Value = string.Empty;

            State.StringInfo.Value = State.StringInfo.Value + input.StringValue;

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
            State.EnumState.Value = (StateEnum) input.Int32Value;
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
            {
                State.MappedState[input.ProtobufValue.Int64Value] =
                    new ProtobufMessage
                    {
                        BoolValue = true,
                        Int64Value = input.ProtobufValue.Int64Value,
                        StringValue = input.ProtobufValue.StringValue
                    };
            }
            else
            {
                State.MappedState[input.ProtobufValue.Int64Value] =
                    new ProtobufMessage
                    {
                        BoolValue = true,
                        Int64Value = State.Complex3Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue]
                            .Int64Value
                            .Add(input.ProtobufValue.Int64Value),
                        StringValue = input.ProtobufValue.StringValue
                    };
            }
            
            return new Empty();
        }

        public override Empty TestMapped1State(ProtobufInput input)
        {
            var protobufMessage = State.Complex3Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue];
            if (protobufMessage == null)
            {
                State.Complex3Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue] =
                    new ProtobufMessage()
                    {
                        BoolValue = true,
                        Int64Value = input.ProtobufValue.Int64Value,
                        StringValue = input.ProtobufValue.StringValue
                    };
            }
            else
            {
                State.Complex3Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue] =
                    new ProtobufMessage
                    {
                        BoolValue = true,
                        Int64Value = State.Complex3Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue]
                            .Int64Value
                            .Add(input.ProtobufValue.Int64Value),
                        StringValue = input.ProtobufValue.StringValue
                    };
            }

            return new Empty();
        }

        public override Empty TestMapped2State(ProtobufInput input)
        {
            var protobufMessage =
                State.Complex4Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue][
                    input.ProtobufValue.StringValue];
            if (protobufMessage == null)
            {
                State.Complex4Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue][input.ProtobufValue.StringValue] =
                    new ProtobufMessage
                    {
                        BoolValue = true,
                        Int64Value = input.ProtobufValue.Int64Value,
                        StringValue = input.ProtobufValue.StringValue
                    };
            }
            else
            {
                State.Complex4Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue][input.ProtobufValue.StringValue] =
                    new ProtobufMessage
                    {
                        BoolValue = true,
                        Int64Value = State.Complex3Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue]
                            .Int64Value
                            .Add(input.ProtobufValue.Int64Value),
                        StringValue = input.ProtobufValue.StringValue
                    };
            }

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
            _number = input.Int32Value;
            _field1 = input.Int64Value;
            _field2 = input.StringValue;
            _field3 = input.BoolValue;
            State.Int64Info.Value = Number;
            State.StringInfo.Value = String;
            return new Empty();
        }

        public override ResetNestedOutput TestResetNestedFields(ResetNestedInput input)
        {
            _innerContractType = new InnerContractType();
            _innerContractType.SetValue(input.Int32Value, input.StringValue);
            var number = _innerContractType.CheckNumberValue();
            var staticString = _innerContractType.CheckStaticValue();

            return new ResetNestedOutput
            {
                Int32Value = number,
                StringValue = staticString
            };
        }

        public override ResetOtherTypeNestedOutput TestResetOtherTypeFields(ResetNestedInput input)
        {
            _basicTestType = new BasicContractTestType();
            _basicTestType.SetBasicContractTestType(input.Int32Value);
            var s = _basicTestType.CheckFunc().Invoke(input.StringValue);
            var testType = _basicTestType.CheckTypeValue();
            _basicTestType.SetStaticField();
            BasicContractTestType.BasicContractTestTypePublicStaticField = new BasicContractTestType();

            var innerTestTypeObj = new BasicContractTestType.InnerTestType(1);
            innerTestTypeObj.SetStaticField();
            BasicContractTestType.InnerTestType.InnerTestTypePublicStaticField =
                new BasicContractTestType.InnerTestType(2);

            var innerContractTypeObj = new InnerContractType();
            innerContractTypeObj.SetStaticField();
            InnerContractType.InnerContractTypePublicStaticField = new InnerContractType();

            return new ResetOtherTypeNestedOutput
            {
                TypeConst = testType.CheckConstNumberValue(),
                TypeNumber = testType.CheckNumberValue(),
                BasicTypeNumber = _basicTestType.CheckNumberValue(),
                BasicTypeStaticNumber = _basicTestType.CheckStaticNumberValue(),
                StringValue = s
            };
        }

        public class InnerContractType
        {
            private int _testTypeNumber;
            private static string _testTypeString;

            private static InnerContractType _innerContractTypePrivateStaticField;
            public static InnerContractType InnerContractTypePublicStaticField;

            public void SetValue(int number, string s)
            {
                _testTypeNumber = number;
                _testTypeString = s;
            }

            public int CheckNumberValue()
            {
                return _testTypeNumber;
            }

            public string CheckStaticValue()
            {
                return _testTypeString;
            }

            public void SetStaticField()
            {
                _innerContractTypePrivateStaticField = this;
            }

            public static bool CheckAllStaticFieldsReset()
            {
                return _testTypeString == null && _innerContractTypePrivateStaticField == null &&
                       InnerContractTypePublicStaticField == null;
            }
        }
    }
}