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