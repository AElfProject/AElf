using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.BasicFunction;

/// <summary>
///     Action methods
/// </summary>
public partial class BasicFunctionContract : BasicFunctionContractContainer.BasicFunctionContractBase
{
    public override Empty InitialBasicFunctionContract(InitialBasicContractInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        Assert(input.MinValue > 0 && input.MaxValue > 0 && input.MaxValue >= input.MinValue,
            "Invalid min/max value input setting.");

        State.Initialized.Value = true;
        State.ContractName.Value = input.ContractName;
        State.ContractManager.Value = input.Manager;
        State.MinBet.Value = input.MinValue;
        State.MaxBet.Value = input.MaxValue;
        State.MortgageBalance.Value = input.MortgageValue;

        return new Empty();
    }

    public override Empty UpdateBetLimit(BetLimitInput input)
    {
        Assert(Context.Sender == State.ContractManager.Value, "Only manager can perform this action.");
        Assert(input.MinValue > 0 && input.MaxValue > 0 && input.MaxValue >= input.MinValue,
            "Invalid min/max value input setting.");

        State.MinBet.Value = input.MinValue;
        State.MaxBet.Value = input.MaxValue;

        return new Empty();
    }

    public override Empty UserPlayBet(BetInput input)
    {
        Assert(input.Int64Value >= State.MinBet.Value && input.Int64Value <= State.MaxBet.Value,
            $"Input balance not in boundary({State.MinBet.Value}, {State.MaxBet.Value}).");
        //Assert(input.Int64Value > State.WinerHistory[Context.Sender], "Should bet bigger than your reward money.");
        State.TotalBetBalance.Value = State.TotalBetBalance.Value.Add(input.Int64Value);

        var result = WinOrLose(input.Int64Value);

        if (result == 0)
        {
            State.LoserHistory[Context.Sender] = State.LoserHistory[Context.Sender].Add(input.Int64Value);
        }
        else
        {
            State.RewardBalance.Value = State.RewardBalance.Value.Add(result);
            State.WinerHistory[Context.Sender] = State.WinerHistory[Context.Sender].Add(result);
        }

        return new Empty();
    }

    public override Empty LockToken(LockTokenInput input)
    {
        State.TokenContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

        State.TokenContract.Lock.Send(new LockInput
        {
            Symbol = input.Symbol,
            Address = input.Address,
            Amount = input.Amount,
            LockId = input.LockId,
            Usage = input.Usage
        });

        return new Empty();
    }

    public override Empty UnlockToken(UnlockTokenInput input)
    {
        State.TokenContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

        State.TokenContract.Unlock.Send(new UnlockInput
        {
            Symbol = input.Symbol,
            Address = input.Address,
            Amount = input.Amount,
            LockId = input.LockId,
            Usage = input.Usage
        });
        return new Empty();
    }

    public override GetLockedTokenAmountOutput GetLockedAmount(GetLockedTokenAmountInput input)
    {
        State.TokenContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        var output = State.TokenContract.GetLockedAmount.Call(new GetLockedAmountInput
        {
            Symbol = input.Symbol,
            Address = input.Address,
            LockId = input.LockId
        });
        return new GetLockedTokenAmountOutput
        {
            Address = output.Address,
            Symbol = output.Symbol,
            LockId = output.LockId,
            Amount = output.Amount
        };
    }

    public override Empty ValidateOrigin(Address address)
    {
        Assert(address == Context.Origin, "Validation failed, origin is not expected.");
        return new Empty();
    }

    private long WinOrLose(long betAmount)
    {
        var data = State.TotalBetBalance.Value.Sub(State.RewardBalance.Value);
        if (data < 0)
            data = data.Mul(-1);

        if (data % 100 == 1)
            return betAmount.Mul(1000);
        if (data % 50 == 5)
            return betAmount.Mul(50);
        return 0;
    }

    public override Empty TransferTokenToContract(TransferTokenToContractInput input)
    {
        State.TokenContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

        State.TokenContract.TransferToContract.Send(new TransferToContractInput
        {
            Symbol = input.Symbol,
            Amount = input.Amount,
            Memo = input.Memo
        });

        return new Empty();
    }
    

    public override Empty CreateTokenThroughMultiToken(CreateTokenThroughMultiTokenInput input)
    {
        if (State.TokenContract.Value == null)
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
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

        State.TokenContract.Create.Send(new CreateInput
        {
            Symbol = input.Symbol,
            Decimals = input.Decimals,
            Issuer = input.Issuer,
            IsBurnable = input.IsBurnable,
            IssueChainId = input.IssueChainId,
            TokenName = input.TokenName,
            TotalSupply = input.TotalSupply,
            ExternalInfo = new MultiToken.ExternalInfo
            {
                Value = { input.ExternalInfo.Value }
            },
            LockWhiteList = { input.LockWhiteList }
        });
        
        return new Empty();
    }

}