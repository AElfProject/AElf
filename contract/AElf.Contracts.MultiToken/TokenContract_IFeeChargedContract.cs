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
            AssertValidTokens(input.AvailableSymbols.Append(input.BaseSymbol), input.BaseAmount);
            State.MethodFees[input.Method] = new TokenAmount
            {
                BaseSymbol = input.BaseSymbol,
                AvailableSymbols = { input.AvailableSymbols},
                BaseAmount = input.BaseAmount
            };
            return new Empty();
        }
    }
}