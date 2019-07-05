using Acs1;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContract
    {
        public override TokenAmount GetMethodFee(MethodName input)
        {
            var tokenAmount = State.TransactionFees[input.Name];
            if (tokenAmount != null)
            {
                return tokenAmount;
            }

            switch (input.Name)
            {
                case nameof(CreateProfitItem):
                    return new TokenAmount
                    {
                        SymbolToAmount = {{Context.Variables.NativeSymbol, 10_00000000}}
                    };
                default:
                    return new TokenAmount
                    {
                        SymbolToAmount = {{Context.Variables.NativeSymbol, 1_00000000}}
                    };
            }
        }

        public override Empty SetMethodFee(SetMethodFeeInput input)
        {
            if (State.ParliamentAuthContract.Value == null)
            {
                State.ParliamentAuthContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentAuthContractSystemName);
            }

            Assert(Context.Sender == State.ParliamentAuthContract.GetDefaultOrganizationAddress.Call(new Empty()));
            State.TransactionFees[input.Method] = new TokenAmount {SymbolToAmount = {input.SymbolToAmount}};

            return new Empty();
        }
    }
}