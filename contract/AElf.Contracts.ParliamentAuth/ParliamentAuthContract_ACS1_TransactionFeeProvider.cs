using Acs1;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.ParliamentAuth
{
    public partial class ParliamentAuthContract
    {
        public override TokenAmounts GetMethodFee(MethodName input)
        {
            var tokenAmounts = State.TransactionFees[input.Name];
            if (tokenAmounts != null)
            {
                return tokenAmounts;
            }

            switch (input.Name)
            {
                default:
                    return new TokenAmounts
                    {
                        Amounts = {new TokenAmount {Symbol = Context.Variables.NativeSymbol, Amount = 1_00000000}}
                    };
            }
        }

        public override Empty SetMethodFee(TokenAmounts input)
        {
            Assert(Context.Sender == GetGenesisOwnerAddress(new Empty()));
            State.TransactionFees[input.Method] = input;

            return new Empty();
        }
    }
}