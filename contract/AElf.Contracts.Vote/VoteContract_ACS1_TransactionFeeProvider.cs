using Acs1;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Vote
{
    public partial class VoteContract
    {
        public override MethodFees GetMethodFee(StringValue input)
        {
            var tokenAmounts = State.TransactionFees[input.Value];
            if (tokenAmounts != null)
            {
                return tokenAmounts;
            }

            switch (input.Value)
            {
                case nameof(Register):
                    return new MethodFees
                    {
                        Fees =
                        {
                            new MethodFee {Symbol = Context.Variables.NativeSymbol, BasicFee = 10_00000000}
                        }
                    };
                default:
                    return new MethodFees
                    {
                        Fees =
                        {
                            new MethodFee {Symbol = Context.Variables.NativeSymbol, BasicFee = 1_00000000}
                        }
                    };
            }
        }

        public override Empty SetMethodFee(MethodFees input)
        {
            if (State.ParliamentContract.Value == null)
            {
                State.ParliamentContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
            }

            Assert(Context.Sender == State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty()));
            State.TransactionFees[input.MethodName] = input;

            return new Empty();
        }
    }
}