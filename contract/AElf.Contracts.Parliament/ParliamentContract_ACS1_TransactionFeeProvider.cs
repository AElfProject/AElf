using Acs1;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Parliament
{
    public partial class ParliamentContract
    {
        public override MethodFees GetMethodFee(StringValue input)
        {
            return State.TransactionFees[input.Value];
        }

        public override Empty SetMethodFee(MethodFees input)
        {
            Assert(Context.Sender == GetDefaultOrganizationAddress(new Empty()));
            State.TransactionFees[input.MethodName] = input;

            return new Empty();
        }
    }
}