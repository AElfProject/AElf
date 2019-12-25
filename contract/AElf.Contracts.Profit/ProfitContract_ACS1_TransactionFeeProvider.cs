using Acs1;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContract
    {
        public override MethodFees GetMethodFee(StringValue input)
        {
            var methodFees = State.TransactionFees[input.Value];
            if (methodFees != null)
            {
                return methodFees;
            }

            switch (input.Value)
            {
                case nameof(CreateScheme):
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
            ValidateContractState(State.ParliamentContract, SmartContractConstants.ParliamentContractSystemName);

            Assert(Context.Sender == State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty()));
            Assert(input.Fees.Count <= ProfitContractConstants.TokenAmountLimit, "Invalid input.");
            State.TransactionFees[input.MethodName] = input;

            return new Empty();
        }
    }
}