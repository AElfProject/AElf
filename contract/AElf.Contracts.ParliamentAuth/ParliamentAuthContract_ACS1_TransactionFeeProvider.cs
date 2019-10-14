using Acs1;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.ParliamentAuth
{
    public partial class ParliamentAuthContract
    {
        public override TokenAmounts GetMethodFee(MethodName input)
        {
            return State.TransactionFees[input.Name];
        }

        public override Empty SetMethodFee(TokenAmounts input)
        {
            Assert(Context.Sender == GetGenesisOwnerAddress(new Empty()));
            State.TransactionFees[input.Method] = input;

            return new Empty();
        }
    }
}