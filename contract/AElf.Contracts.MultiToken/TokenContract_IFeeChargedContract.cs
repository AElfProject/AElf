using System.Linq;
using Acs1;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract
    {
        [View]
        public override TokenAmount GetMethodFee(MethodName input)
        {
            return State.MethodFees[input.Name];
        }

        public override Empty SetMethodFee(SetMethodFeeInput input)
        {
            foreach (var symbolToAmount in input.SymbolToAmount)
            {
                AssertValidToken(symbolToAmount.Key, symbolToAmount.Value);
            }

            State.MethodFees[input.Method] = new TokenAmount
            {
                SymbolToAmount = {input.SymbolToAmount}
            };
            return new Empty();
        }
    }
}