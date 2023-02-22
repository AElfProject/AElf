using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenHolder;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS9;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.DApp;

public partial class DAppContract : DAppContainer.DAppBase
{
    //just for unit cases
    public override Empty InitializeForUnitTest(InitializeInput input)
    {
        State.TokenHolderContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName);
        State.TokenContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        State.DividendPoolContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
        State.Symbol.Value = input.Symbol == string.Empty ? "APP" : input.Symbol;
        State.ProfitReceiver.Value = input.ProfitReceiver;
        
        ApproveTokenForCreateToken();
        CreateToken(input.ProfitReceiver, true);
        // To test TokenHolder Contract.
        CreateTokenHolderProfitScheme();
        // To test ACS9 workflow.
        SetProfitConfig();
        State.ProfitReceiver.Value = input.ProfitReceiver;
        return new Empty();
    }

    public override Empty Initialize(InitializeInput input)
    {
        State.TokenHolderContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName);
        State.TokenContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        State.DividendPoolContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
        State.Symbol.Value = input.Symbol == string.Empty ? "APP" : input.Symbol;
        State.ProfitReceiver.Value = input.ProfitReceiver;
        
        ApproveTokenForCreateToken();
        CreateToken(input.ProfitReceiver);
        CreateTokenHolderProfitScheme();
        SetProfitConfig();
        State.ProfitReceiver.Value = input.ProfitReceiver;
        return new Empty();
    }

    /// <summary>
    ///     When user sign up, give him 10 APP tokens, then initialize his profile.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Empty SignUp(Empty input)
    {
        Assert(State.Profiles[Context.Sender] == null, "Already registered.");
        var profile = new Profile
        {
            UserAddress = Context.Sender
        };
        State.TokenContract.Issue.Send(new IssueInput
        {
            Symbol = State.Symbol.Value,
            Amount = DAppConstants.ForNewUser,
            To = Context.Sender
        });

        // Update profile.
        profile.Records.Add(new Record
        {
            Type = RecordType.SignUp,
            Timestamp = Context.CurrentBlockTime,
            Description = $"{State.Symbol.Value} +{DAppConstants.ForNewUser}"
        });
        State.Profiles[Context.Sender] = profile;

        return new Empty();
    }

    public override Empty Deposit(DepositInput input)
    {
        // User Address -> DApp Contract.
        State.TokenContract.TransferToContract.Send(new TransferToContractInput
        {
            Symbol = "ELF",
            Amount = input.Amount
        });

        State.TokenContract.Issue.Send(new IssueInput
        {
            Symbol = State.Symbol.Value,
            Amount = input.Amount,
            To = Context.Sender
        });

        // Update profile.
        var profile = State.Profiles[Context.Sender];
        profile.Records.Add(new Record
        {
            Type = RecordType.Deposit,
            Timestamp = Context.CurrentBlockTime,
            Description = $"{State.Symbol.Value} +{input.Amount}"
        });
        State.Profiles[Context.Sender] = profile;

        return new Empty();
    }

    public override Empty Withdraw(WithdrawInput input)
    {
        State.TokenContract.TransferToContract.Send(new TransferToContractInput
        {
            Symbol = State.Symbol.Value,
            Amount = input.Amount
        });

        State.TokenContract.Transfer.Send(new TransferInput
        {
            To = Context.Sender,
            Symbol = input.Symbol,
            Amount = input.Amount
        });

        State.TokenHolderContract.RemoveBeneficiary.Send(new RemoveTokenHolderBeneficiaryInput
        {
            Beneficiary = Context.Sender,
            Amount = input.Amount
        });

        // Update profile.
        var profile = State.Profiles[Context.Sender];
        profile.Records.Add(new Record
        {
            Type = RecordType.Withdraw,
            Timestamp = Context.CurrentBlockTime,
            Description = $"{State.Symbol.Value} -{input.Amount}"
        });
        State.Profiles[Context.Sender] = profile;

        return new Empty();
    }

    public override Empty Use(Record input)
    {
        State.TokenContract.TransferToContract.Send(new TransferToContractInput
        {
            Symbol = State.Symbol.Value,
            Amount = DAppConstants.UseFee
        });
        if (input.Symbol == string.Empty)
            input.Symbol = State.TokenContract.GetPrimaryTokenSymbol.Call(new Empty()).Value;
        var contributeAmount = DAppConstants.UseFee.Div(3);
        State.TokenContract.Approve.Send(new ApproveInput
        {
            Spender = State.TokenHolderContract.Value,
            Symbol = input.Symbol,
            Amount = contributeAmount
        });

        // Contribute 1/3 profits (ELF) to profit scheme.
        State.TokenHolderContract.ContributeProfits.Send(new ContributeProfitsInput
        {
            SchemeManager = Context.Self,
            Amount = contributeAmount,
            Symbol = input.Symbol
        });

        // Update profile.
        var profile = State.Profiles[Context.Sender];
        profile.Records.Add(new Record
        {
            Type = RecordType.Withdraw,
            Timestamp = Context.CurrentBlockTime,
            Description = $"{State.Symbol.Value} -{DAppConstants.UseFee}",
            Symbol = input.Symbol
        });
        State.Profiles[Context.Sender] = profile;

        return new Empty();
    }

    private void CreateToken(Address profitReceiver, bool isLockWhiteListIncludingSelf = false)
    {
        var lockWhiteList = new List<Address>
            { Context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName) };
        if (isLockWhiteListIncludingSelf)
            lockWhiteList.Add(Context.Self);
        State.TokenContract.Create.Send(new CreateInput
        {
            Symbol = State.Symbol.Value,
            TokenName = "DApp Token",
            Decimals = DAppConstants.Decimal,
            Issuer = Context.Self,
            IsBurnable = true,
            TotalSupply = DAppConstants.TotalSupply,
            LockWhiteList =
            {
                lockWhiteList
            }
        });

        State.TokenContract.Issue.Send(new IssueInput
        {
            To = profitReceiver,
            Amount = DAppConstants.TotalSupply / 5,
            Symbol = State.Symbol.Value,
            Memo = "Issue token for profit receiver"
        });
    }

    private void CreateTokenHolderProfitScheme()
    {
        State.TokenHolderContract.CreateScheme.Send(new CreateTokenHolderProfitSchemeInput
        {
            Symbol = State.Symbol.Value
        });
    }

    private void SetProfitConfig()
    {
        State.ProfitConfig.Value = new ProfitConfig
        {
            DonationPartsPerHundred = 1,
            StakingTokenSymbol = "APP",
            ProfitsTokenSymbolList = { "ELF" }
        };
    }

    private void ApproveTokenForCreateToken()
    {
        var fee = State.TokenContract.GetMethodFee.Call(new StringValue{Value = "Create"});
        var approveFee = new Dictionary<string, long>();
        if (fee == null || fee.Fees.Count == 0)
        {
            var symbol = Context.Variables.NativeSymbol;
            var feeAmount = 10000_00000000;
            approveFee[symbol] = feeAmount;
        }
        else
        {
            foreach (var f in fee.Fees)
            {
                approveFee[f.Symbol] = f.BasicFee;
            }
        }

        foreach (var (key, value) in approveFee)
        {
            State.TokenContract.Approve.Send(new ApproveInput
            {
                Spender = State.TokenContract.Value,
                Amount = value,
                Symbol = key
            });
        }
    }
}