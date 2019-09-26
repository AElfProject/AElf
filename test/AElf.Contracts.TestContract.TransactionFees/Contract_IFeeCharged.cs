using Acs1;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.TransactionFees
{
    public partial class TransactionFeesContract
    {
        [View]
        public override TokenAmounts GetMethodFee(MethodName input)
        {
            var tokenAmounts = State.MethodFees[input.Name];
            if (tokenAmounts != null)
                return tokenAmounts;
            
            //set default tx fee
            return new TokenAmounts
            {
                Amounts =
                {
                    new TokenAmount
                    {
                        Symbol = "ELF",
                        Amount = 1_00000000
                    }
                }
            };
        }

        public override Empty SetMethodFee(TokenAmounts input)
        {
            State.MethodFees[input.Method] = input;

            return new Empty();
        }
    }
}