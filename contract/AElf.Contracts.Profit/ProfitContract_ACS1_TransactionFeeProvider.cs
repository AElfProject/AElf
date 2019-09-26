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
                        Fee =
                        {
                            new MethodFee {Symbol = Context.Variables.NativeSymbol, BasicFee = 10_00000000}
                        }
                    };
                default:
                    return new MethodFees
                    {
                        Fee =
                        {
                            new MethodFee {Symbol = Context.Variables.NativeSymbol, BasicFee = 1_00000000}
                        }
                    };
            }
        }

        public override Empty SetMethodFee(MethodFees input)
        {
            if (State.ParliamentAuthContract.Value == null)
            {
                State.ParliamentAuthContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentAuthContractSystemName);
            }

            Assert(Context.Sender == State.ParliamentAuthContract.GetGenesisOwnerAddress.Call(new Empty()));
            State.TransactionFees[input.MethodName] = input;

            return new Empty();
        }
    }
}