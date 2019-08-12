using System.Linq;
using Acs1;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract
    {
        [View]
        public override TokenAmounts GetMethodFee(MethodName input)
        {
            return State.MethodFees[input.Name];
        }

        public override Empty SetMethodFee(TokenAmounts input)
        {
            foreach (var symbolToAmount in input.Amounts)
            {
                AssertValidToken(symbolToAmount.Symbol, symbolToAmount.Amount);
            }

            State.MethodFees[input.Method] = input;
            return new Empty();
        }
    }
}