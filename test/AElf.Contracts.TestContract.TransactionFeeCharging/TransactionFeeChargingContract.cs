using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS1;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.TransactionFeeCharging;

public class
    TransactionFeeChargingContract : TransactionFeeChargingContractContainer.TransactionFeeChargingContractBase
{
    public override Empty InitializeTransactionFeeChargingContract(
        InitializeTransactionFeeChargingContractInput input)
    {
        State.TokenContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        State.TokenContract.Approve.Send(new ApproveInput
        {
            Spender = State.TokenContract.Value,
            Amount = 10000_00000000,    
            Symbol = "ELF"
        });
        State.TokenContract.Create.Send(new CreateInput
        {
            Symbol = input.Symbol,
            TokenName = "Token of Transaction Fee Charging Contract",
            Decimals = 2,
            Issuer = Context.Self,
            IsBurnable = true,
            TotalSupply = TransactionFeeChargingContractConstants.TotalSupply,
            LockWhiteList =
            {
                Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName),
                Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName)
            }
        });
        State.TokenContract.Issue.Send(new IssueInput
        {
            Symbol = input.Symbol,
            Amount = TransactionFeeChargingContractConstants.AmountIssueToTokenConverterContract,
            To = Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName)
        });
        return new Empty();
    }

    public override Empty SetMethodFee(MethodFees input)
    {
        State.TransactionFees[input.MethodName] = input;
        return new Empty();
    }

    public override MethodFees GetMethodFee(StringValue input)
    {
        return State.TransactionFees[input.Value];
    }

    public override Empty SendForFun(Empty input)
    {
        return new Empty();
    }

    public override Empty SupposedToFail(Empty input)
    {
        Assert(false, "Fate!");
        return new Empty();
    }

    public override Empty IssueToTokenConvert(IssueAmount input)
    {
        State.TokenContract.Issue.Send(new IssueInput
        {
            Symbol = input.Symbol,
            Amount = input.Amount,
            To = Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName)
        });
        return new Empty();
    }
}