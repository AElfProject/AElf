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
            AssertValidToken(input.Symbol, input.Amount);
            State.MethodFees[input.Method] = new TokenAmount()
            {
                Symbol = input.Symbol,
                Amount = input.Amount
            };
            return new Empty();
        }
    }
}