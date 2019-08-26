using Acs1;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Vote
{
    public partial class VoteContract
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
                case nameof(Register):
                    return new TokenAmounts
                    {
                        Amounts = {new TokenAmount {Symbol = Context.Variables.NativeSymbol, Amount = 10_00000000}}
                    };
                default:
                    return new TokenAmounts
                    {
                        Amounts = {new TokenAmount {Symbol = Context.Variables.NativeSymbol, Amount = 1_00000000}}
                    };
            }
        }

        public override Empty SetMethodFee(TokenAmounts input)
        {
            if (State.ParliamentAuthContract.Value == null)
            {
                State.ParliamentAuthContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentAuthContractSystemName);
            }

            Assert(Context.Sender == State.ParliamentAuthContract.GetGenesisOwnerAddress.Call(new Empty()));
            State.TransactionFees[input.Method] = input;

            return new Empty();
        }
    }
}