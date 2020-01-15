using Acs1;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.DApp
{
    public partial class DAppContract
    {
        public override MethodFees GetMethodFee(StringValue input)
        {
            var methodFees = State.MethodFees[input.Value];
            if (methodFees != null)
            {
                return methodFees;
            }
            
            return new MethodFees();
        }

        public override Empty SetMethodFee(MethodFees input)
        {
            State.MethodFees[input.MethodName] = input;
            
            return new Empty();
        }
    }
}