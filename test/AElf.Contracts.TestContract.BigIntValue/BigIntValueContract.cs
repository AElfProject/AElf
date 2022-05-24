using AElf.CSharp.Core;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.BigIntValue
{
    public class BigIntValueContract : BigIntValueContractContainer.BigIntValueContractBase
    {
        public override Empty Add(BigIntValueInput input)
        {
            State.BigIntState.Value = input.Foo.Add(input.Bar);
            return new Empty();
        }

        public override Empty Sub(BigIntValueInput input)
        {
            State.BigIntState.Value = input.Foo.Sub(input.Bar);
            return new Empty();
        }

        public override Empty Mul(BigIntValueInput input)
        {
            State.BigIntState.Value = input.Foo.Mul(input.Bar);
            return new Empty();
        }

        public override Empty Div(BigIntValueInput input)
        {
            State.BigIntState.Value = input.Foo.Div(input.Bar);
            return new Empty();
        }

        public override Empty Pow(BigIntValuePowInput input)
        {
            State.BigIntState.Value = input.Value.Pow(input.Exponent);
            return new Empty();
        }

        public override Types.BigIntValue Get(Empty input)
        {
            return State.BigIntState.Value;
        }
    }
}