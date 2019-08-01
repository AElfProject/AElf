using System.Linq;
using Acs1;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract
    {
        [View]
        public override TokenAmounts GetMethodFee(MethodName input)
        {
            return State.MethodFees[input.Name];
        }

        public override Empty SetMethodFee(TokenAmounts input)
        {
            if (State.ParliamentAuthContract.Value == null)
            {
                State.ParliamentAuthContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentAuthContractSystemName);
            }

            var genesisOwnerAddress = State.ParliamentAuthContract.GetGenesisOwnerAddress.Call(new Empty());
            Assert(Context.Sender == genesisOwnerAddress, "No permission to set method fee.");

            foreach (var symbolToAmount in input.Amounts)
            {
                AssertValidToken(symbolToAmount.Symbol, symbolToAmount.Amount);
            }

            State.MethodFees[input.Method] = input;
            return new Empty();
        }
    }
}