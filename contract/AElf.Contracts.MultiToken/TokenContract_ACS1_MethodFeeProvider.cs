using Acs1;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract
    {
        private MethodFees GetMethodFee(string input)
        {
            if (input == nameof(Transfer))
            {
                return new MethodFees
                {
                    MethodName = nameof(Transfer),
                    Fee =
                    {
                        new MethodFee
                        {
                            Symbol = Context.Variables.NativeSymbol,
                            BasicFee = 1_00000000
                        }
                    }
                };
            }

            return State.MethodFees[input];
        }

        internal Empty SetMethodFee(MethodFees input)
        {
            if (State.ParliamentAuthContract.Value == null)
            {
                State.ParliamentAuthContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentAuthContractSystemName);
            }

            // Parliament Auth Contract maybe not deployed.
            if (State.ParliamentAuthContract.Value != null)
            {
                var genesisOwnerAddress = State.ParliamentAuthContract.GetGenesisOwnerAddress.Call(new Empty());
                Assert(Context.Sender == genesisOwnerAddress, "No permission to set method fee.");
            }

            foreach (var symbolToAmount in input.Fee)
            {
                AssertValidToken(symbolToAmount.Symbol, symbolToAmount.BasicFee);
            }

            State.MethodFees[input.MethodName] = input;
            return new Empty();
        }
    }
}