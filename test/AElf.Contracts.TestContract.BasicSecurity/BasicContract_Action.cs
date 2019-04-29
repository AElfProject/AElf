using System;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.BasicSecurity
{
    public partial class BasicSecurityContract : BasicSecurityContractContainer.BasicSecurityContractBase
    {
        public override Empty InitialBasicSecurityContract(Address input)
        {
            Assert(!State.Initialized.Value, "Already initialized."); 
            
            //set basic1 contract reference
            Assert(input != null, "Basic1Contract address is not exist.");
            State.BasicFunctionTestContract.Value = input;
            
            State.Initialized.Value = true;
            State.BoolInfo.Value = true;
            State.Int32Info.Value = 0;
            State.UInt32Info.Value = 0;
            State.Int64Info.Value = 0;
            State.UInt64Info.Value = 0;
            State.StringInfo.Value = String.Empty;
            State.BytesInfo.Value = new byte[]{};
            
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
            if(string.IsNullOrEmpty(State.StringInfo.Value))
                State.StringInfo.Value = string.Empty;
            
            State.StringInfo.Value = State.StringInfo.Value + input.StringValue;
            
            return new Empty();
        }

        public override Empty TestBytesState(BytesInput input)
        {
            State.BytesInfo.Value = input.BytesValue.ToByteArray();
            
            return new Empty();
        }

        public override Empty TestProtobufState(ProtobufInput input)
        {
            State.ProtoInfo2.Value = input.ProtobufValue;
            
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

        public override Empty TestMapped1State(ProtobufInput input)
        {
            var protobufMessage = State.Complex3Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue];
            if(protobufMessage == null)
            {    State.Complex3Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue] = new ProtobufMessage()
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
                        Int64Value = State.Complex3Info[input.ProtobufValue.Int64Value][input.ProtobufValue.StringValue].Int64Value
                            .Add(input.ProtobufValue.Int64Value),
                        StringValue = input.ProtobufValue.StringValue
                    };
            }

          return new Empty();
        }

        public override Empty TestMapped2State(Complex3Input input)
        {
            var tradeMessage = State.Complex4Info[input.From][input.PairA][input.To][input.PairB];
            if (tradeMessage == null)
            {
                input.TradeDetails.Timestamp = Context.CurrentBlockTime.ToTimestamp();
                State.Complex4Info[input.From][input.PairA][input.To][input.PairB] = input.TradeDetails;
            }
            else
            {
                tradeMessage.FromAmount += input.TradeDetails.FromAmount;
                tradeMessage.ToAmount += input.TradeDetails.ToAmount;
                tradeMessage.Timestamp = Context.CurrentBlockTime.ToTimestamp();

                State.Complex4Info[input.From][input.PairA][input.To][input.PairB] = tradeMessage;
            }
            
            return new Empty();
        }

        //Reference call action
        public override Empty TestExecuteExternalMethod(Int64Input input)
        {
            var feeValue = input.Int64Value * 5 / 100;
            var betValue = input.Int64Value.Sub(feeValue);
            
            State.Int64Info.Value.Add(feeValue);
            State.BasicFunctionTestContract.UserPlayBet.Send(new BetInput
            {
                Int64Value = betValue
            });
            
            return new Empty();
        }
    }
}