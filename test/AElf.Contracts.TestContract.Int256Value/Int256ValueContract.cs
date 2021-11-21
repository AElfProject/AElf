using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.Int256Value
{
    public class Int256ValueContract : Int256ValueContractContainer.Int256ValueContractBase
    {
        public override Empty Int256ValueAdd(Int256ValueInput input)
        {
            State.Int256State.Value = input.Foo.Add(input.Bar);
            return new Empty();
        }

        public override Empty Int256ValueSub(Int256ValueInput input)
        {
            State.Int256State.Value = input.Foo.Sub(input.Bar);
            return new Empty();
        }

        public override Empty Int256ValueMul(Int256ValueInput input)
        {
            State.Int256State.Value = input.Foo.Mul(input.Bar);
            return new Empty();
        }

        public override Empty Int256ValueDiv(Int256ValueInput input)
        {
            State.Int256State.Value = input.Foo.Div(input.Bar);
            return new Empty();
        }

        public override Empty Int256ValuePow(Int256ValuePowInput input)
        {
            State.Int256State.Value = input.Value.Pow(input.Exponent);
            return new Empty();
        }

        public override Empty UInt256ValueAdd(UInt256ValueInput input)
        {
            State.UInt256State.Value = input.Foo.Add(input.Bar);
            return new Empty();
        }

        public override Empty UInt256ValueSub(UInt256ValueInput input)
        {
            State.UInt256State.Value = input.Foo.Sub(input.Bar);
            return new Empty();
        }

        public override Empty UInt256ValueMul(UInt256ValueInput input)
        {
            State.UInt256State.Value = input.Foo.Mul(input.Bar);
            return new Empty();
        }

        public override Empty UInt256ValueDiv(UInt256ValueInput input)
        {
            State.UInt256State.Value = input.Foo.Div(input.Bar);
            return new Empty();
        }

        public override Empty UInt256ValuePow(UInt256ValuePowInput input)
        {
            State.UInt256State.Value = input.Value.Pow(input.Exponent);
            return new Empty();
        }

        public override Types.Int256Value GetInt256StateValue(Empty input)
        {
            return State.Int256State.Value;
        }

        public override UInt256Value GetUInt256StateValue(Empty input)
        {
            return State.UInt256State.Value;
        }
    }
}