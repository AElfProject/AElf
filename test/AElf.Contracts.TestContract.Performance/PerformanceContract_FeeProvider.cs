using Acs1;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.Performance
{
    public partial class PerformanceContract
    {
        public override MethodFees GetMethodFee(StringValue input)
        {
            var methodFees = State.TransactionFees[input.Value];
            if (methodFees != null)
            {
                return methodFees;
            }

            return new MethodFees
            {
                Fees =
                {
                    new MethodFee {Symbol = Context.Variables.NativeSymbol, BasicFee = 1000_0000} //default 0.1 native symbol
                }
            };
        }

        public override Empty SetMethodFee(MethodFees input)
        {
            if (State.ParliamentAuthContract.Value == null)
            {
                State.ParliamentAuthContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentAuthContractSystemName);
            }

            Assert(Context.Sender == State.ParliamentAuthContract.GetDefaultOrganizationAddress.Call(new Empty()));
            State.TransactionFees[input.MethodName] = input;

            return new Empty();
        }
    }
}