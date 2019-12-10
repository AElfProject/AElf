using System.Collections.Generic;
using System.Linq;
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
                var methodFees = State.MethodFees[input.Value];
                if (methodFees == null) return new MethodFees();
                var symbols = GetMethodFeeSymbols();
                if(symbols.Count >0)
                    Context.LogDebug(()=>$"## {symbols.Count}");
                
                var fees = methodFees.Fees.Where(f => symbols.Contains(f.Symbol));
                Context.LogDebug(()=>$"## Symbols: {symbols[0]} MethodFees: {methodFees} Fees:{fees.Count()}");
                return new MethodFees
                {
                    MethodName = input.Value,
                    Fees =
                    {
                        fees
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
                var genesisOwnerAddress = State.ParliamentAuthContract.GetDefaultOrganizationAddress.Call(new Empty());
                Assert(Context.Sender == genesisOwnerAddress, "No permission to set method fee.");
            }

            foreach (var symbolToAmount in input.Fees)
            {
                Context.LogDebug(()=>$"## {symbolToAmount}");
                AssertValidToken(symbolToAmount.Symbol, symbolToAmount.BasicFee);
            }

            State.MethodFees[input.MethodName] = input;
            Context.LogDebug(()=>$"## Name: {input}");
            return new Empty();
        }

        private List<string> GetMethodFeeSymbols()
        {
            var symbols = new List<string>();
            var primaryTokenSymbol = GetPrimaryTokenSymbol(new Empty()).Value;
            if (primaryTokenSymbol != string.Empty) symbols.Add(primaryTokenSymbol);
            return symbols;
        }
    }
}