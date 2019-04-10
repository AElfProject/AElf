using Acs1;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract
    {
        [View]
        public override GetMethodFeeOutput GetMethodFee(GetMethodFeeInput input)
        {
            return new GetMethodFeeOutput()
            {
                Method = input.Method,
                Fee = State.MethodFees[input.Method]
            };
        }
        
        public override Empty SetMethodFee(SetMethodFeeInput input)
        {
            State.MethodFees[input.Method] = input.Fee;
            return new Empty();
        }
    }
}