using Acs1;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract
    {
        public override MethodFees GetMethodFee(StringValue input)
        {
            return new MethodFees
            {
                MethodName = input.Value,
                Fee =
                {
                    new MethodFee
                    {
                        Symbol = Context.Variables.NativeSymbol,
                        BasicFee = 0
                    }
                }
            };
            if (input.Value == nameof(Transfer))
            {
                return new MethodFees
                {
                    MethodName = nameof(Transfer),
                    Fee =
                    {
                        new MethodFee
                        {
                            Symbol = Context.Variables.NativeSymbol,
                            BasicFee = 0
                        }
                    }
                };
            }

            return State.MethodFees[input.Value];
        }

        public override Empty SetMethodFee(MethodFees input)
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