using Acs1;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract
    {
        public override MethodFees GetMethodFee(StringValue input)
        {
            var officialTokenContractAddress =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            var primaryTokenSymbol =
                Context.Call<StringValue>(officialTokenContractAddress, nameof(GetPrimaryTokenSymbol), new Empty())
                    .Value;
            if (primaryTokenSymbol == string.Empty)
            {
                return new MethodFees();
            }

            if (input.Value == nameof(Transfer) || input.Value == nameof(TransferFrom))
            {
                return new MethodFees
                {
                    MethodName = input.Value,
                    Fees =
                    {
                        new MethodFee
                        {
                            Symbol = primaryTokenSymbol,
                            BasicFee = 1000_0000
                        }
                    }
                };
            }

            return State.MethodFees[input.Value] ?? new MethodFees();
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

            foreach (var symbolToAmount in input.Fees)
            {
                AssertValidToken(symbolToAmount.Symbol, symbolToAmount.BasicFee);
            }

            State.MethodFees[input.MethodName] = input;
            return new Empty();
        }
    }
}