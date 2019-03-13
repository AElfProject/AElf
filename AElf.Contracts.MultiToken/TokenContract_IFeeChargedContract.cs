using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel.Types.SmartContract;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract
    {
        [View]
        public GetMethodFeeOutput GetMethodFee(GetMethodFeeInput input)
        {
            return new GetMethodFeeOutput()
            {
                Method = input.Method,
                Fee = State.MethodFees[input.Method]
            };
        }

        public Nothing SetMethodFee(SetMethodFeeInput input)
        {
            State.MethodFees[input.Method] = input.Fee;
            return Nothing.Instance;
        }
    }
}