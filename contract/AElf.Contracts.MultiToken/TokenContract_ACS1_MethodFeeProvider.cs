using System.Collections.Generic;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken;

public partial class TokenContract
{
    public override Empty SetMethodFee(MethodFees input)
    {
        foreach (var symbolToAmount in input.Fees) AssertValidFeeToken(symbolToAmount.Symbol, symbolToAmount.BasicFee);

        RequiredMethodFeeControllerSet();
        Assert(Context.Sender == State.MethodFeeController.Value.OwnerAddress, "Unauthorized to set method fee.");

        State.TransactionFees[input.MethodName] = input;
        return new Empty();
    }

    public override Empty ChangeMethodFeeController(AuthorityInfo input)
    {
        RequiredMethodFeeControllerSet();
        AssertSenderAddressWith(State.MethodFeeController.Value.OwnerAddress);
        var organizationExist = CheckOrganizationExist(input);
        Assert(organizationExist, "Invalid authority input.");

        State.MethodFeeController.Value = input;
        return new Empty();
    }

    #region Views

    public override MethodFees GetMethodFee(StringValue input)
    {
        if (new List<string>
            {
                nameof(ClaimTransactionFees), nameof(DonateResourceToken), nameof(ChargeTransactionFees),
                nameof(CheckThreshold), nameof(CheckResourceToken), nameof(ChargeResourceToken),
                nameof(CrossChainReceiveToken)
            }.Contains(input.Value))
            return new MethodFees
            {
                MethodName = input.Value,
                IsSizeFeeFree = true
            };
        var fees = State.TransactionFees[input.Value];
        if (input.Value == nameof(Create) && fees == null)
            return new MethodFees
            {
                MethodName = input.Value,
                Fees =
                {
                    new MethodFee
                    {
                        Symbol = Context.Variables.NativeSymbol,
                        BasicFee = 10000_00000000
                    }
                }
            };

        return fees;
    }

    public override AuthorityInfo GetMethodFeeController(Empty input)
    {
        RequiredMethodFeeControllerSet();
        return State.MethodFeeController.Value;
    }

    #endregion

    #region private methods

    private List<string> GetMethodFeeSymbols()
    {
        var symbols = new List<string>();
        var primaryTokenSymbol = GetPrimaryTokenSymbol(new Empty()).Value;
        if (primaryTokenSymbol != string.Empty) symbols.Add(primaryTokenSymbol);
        return symbols;
    }

    private void RequiredMethodFeeControllerSet()
    {
        if (State.MethodFeeController.Value != null) return;
        if (State.ParliamentContract.Value == null)
            State.ParliamentContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);

        var defaultAuthority = new AuthorityInfo();

        // Parliament Auth Contract maybe not deployed.
        if (State.ParliamentContract.Value != null)
        {
            defaultAuthority.OwnerAddress =
                State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());
            defaultAuthority.ContractAddress = State.ParliamentContract.Value;
        }

        State.MethodFeeController.Value = defaultAuthority;
    }

    private void AssertSenderAddressWith(Address address)
    {
        Assert(Context.Sender == address, "Unauthorized behavior.");
    }

    private bool CheckOrganizationExist(AuthorityInfo authorityInfo)
    {
        return Context.Call<BoolValue>(authorityInfo.ContractAddress,
            nameof(AuthorizationContractContainer.AuthorizationContractReferenceState.ValidateOrganizationExist),
            authorityInfo.OwnerAddress).Value;
    }

    private void AssertValidFeeToken(string symbol, long amount)
    {
        AssertValidSymbolAndAmount(symbol, amount);
        if (State.TokenInfos[symbol] == null)
            throw new AssertionException("Token is not found");
        Assert(State.TokenInfos[symbol].IsBurnable, $"Token {symbol} cannot set as method fee.");
    }

    #endregion
}