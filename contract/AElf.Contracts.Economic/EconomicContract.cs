using System.Collections.Generic;
using System.Linq;
using System.Text;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenConverter;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Economic;

public partial class EconomicContract : EconomicContractImplContainer.EconomicContractImplBase
{
    public override Empty InitialEconomicSystem(InitialEconomicSystemInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");

        State.TokenContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

        Context.LogDebug(() => "Will create tokens.");
        CreateNativeToken(input);
        CreateResourceTokens();
        CreateElectionTokens();

        Context.LogDebug(() => "Finished creating tokens.");

        InitialMiningReward(input.MiningRewardTotalAmount);

        RegisterElectionVotingEvent();
        SetTreasurySchemeIdsToElectionContract();

        InitializeTokenConverterContract();
        State.TokenContract.InitialCoefficients.Send(new Empty());
        State.TokenContract.InitializeAuthorizedController.Send(new Empty());
        State.Initialized.Value = true;
        return new Empty();
    }

    private void CreateNativeToken(InitialEconomicSystemInput input)
    {
        var lockWhiteListBackups = new List<Address>
        {
            Context.GetContractAddressByName(SmartContractConstants.VoteContractSystemName),
            Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName),
            Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName),
            Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName),
            Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName),
            Context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName)
        };
        var lockWhiteList = lockWhiteListBackups.Where(address => address != null).ToList();
        State.TokenContract.Create.Send(new CreateInput
        {
            Symbol = input.NativeTokenSymbol,
            TokenName = "Native Token",
            TotalSupply = input.NativeTokenTotalSupply,
            Decimals = input.NativeTokenDecimals,
            IsBurnable = input.IsNativeTokenBurnable,
            Issuer = Context.Self,
            LockWhiteList = { lockWhiteList },
            Owner = Context.Self
        });

        State.TokenContract.SetPrimaryTokenSymbol.Send(new SetPrimaryTokenSymbolInput
            { Symbol = input.NativeTokenSymbol });
    }

    private void CreateResourceTokens()
    {
        var tokenConverter =
            Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName);
        var lockWhiteListBackups = new List<Address>
        {
            Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName),
            Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName)
        };
        var lockWhiteList = lockWhiteListBackups.Where(address => address != null).ToList();
        foreach (var resourceTokenSymbol in Context.Variables
                     .GetStringArray(EconomicContractConstants.PayTxFeeSymbolListName)
                     .Union(Context.Variables.GetStringArray(EconomicContractConstants.PayRentalSymbolListName)))
        {
            State.TokenContract.Create.Send(new CreateInput
            {
                Symbol = resourceTokenSymbol,
                TokenName = $"{resourceTokenSymbol} Token",
                TotalSupply = EconomicContractConstants.ResourceTokenTotalSupply,
                Decimals = EconomicContractConstants.ResourceTokenDecimals,
                Issuer = Context.Self,
                LockWhiteList = { lockWhiteList },
                IsBurnable = true,
                Owner = Context.Self
            });

            State.TokenContract.Issue.Send(new IssueInput
            {
                Symbol = resourceTokenSymbol,
                Amount = EconomicContractConstants.ResourceTokenTotalSupply,
                To = tokenConverter,
                Memo = "Initialize for resource trade"
            });
        }
    }

    private void CreateElectionTokens()
    {
        var lockWhiteListBackups = new List<Address>
        {
            Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName),
            Context.GetContractAddressByName(SmartContractConstants.VoteContractSystemName)
        };
        var lockWhiteList = lockWhiteListBackups.Where(address => address != null).ToList();
        foreach (var symbol in new List<string>
                     { EconomicContractConstants.ElectionTokenSymbol, EconomicContractConstants.ShareTokenSymbol })
        {
            State.TokenContract.Create.Send(new CreateInput
            {
                Symbol = symbol,
                TokenName = $"{symbol} Token",
                TotalSupply = EconomicContractConstants.ElectionTokenTotalSupply,
                Decimals = EconomicContractConstants.ElectionTokenDecimals,
                Issuer = Context.Self,
                IsBurnable = true,
                LockWhiteList = { lockWhiteList },
                Owner = Context.Self
            });
            State.TokenContract.Issue.Send(new IssueInput
            {
                Symbol = symbol,
                Amount = EconomicContractConstants.ElectionTokenTotalSupply,
                To = Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName),
                Memo = "Issue all election tokens to Election Contract."
            });
        }
    }

    /// <summary>
    ///     Only contract owner of Economic Contract can issue native token.
    ///     Mainly for testing.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Empty IssueNativeToken(IssueNativeTokenInput input)
    {
        AssertValidMemo(input.Memo);
        if (State.ZeroContract.Value == null) State.ZeroContract.Value = Context.GetZeroSmartContractAddress();

        var contractOwner = State.ZeroContract.GetContractAuthor.Call(Context.Self);
        if (contractOwner != Context.Sender) return new Empty();

        State.TokenContract.Issue.Send(new IssueInput
        {
            Symbol = Context.Variables.NativeSymbol,
            Amount = input.Amount,
            To = input.To,
            Memo = input.Memo
        });
        return new Empty();
    }

    /// <summary>
    ///     Transfer all the tokens prepared for rewarding mining to consensus contract.
    /// </summary>
    /// <param name="miningRewardAmount"></param>
    private void InitialMiningReward(long miningRewardAmount)
    {
        var consensusContractAddress =
            Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);

        State.TokenContract.Issue.Send(new IssueInput
        {
            To = consensusContractAddress,
            Amount = miningRewardAmount,
            Symbol = Context.Variables.NativeSymbol,
            Memo = "Initial mining reward."
        });
    }

    private void RegisterElectionVotingEvent()
    {
        State.ElectionContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName);
        State.ElectionContract.RegisterElectionVotingEvent.Send(new Empty());
    }

    private void SetTreasurySchemeIdsToElectionContract()
    {
        State.ProfitContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);
        var schemeIdsManagingByTreasuryContract = State.ProfitContract.GetManagingSchemeIds.Call(
            new GetManagingSchemeIdsInput
            {
                Manager = Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName)
            }).SchemeIds;
        var schemeIdsManagingByElectionContract = State.ProfitContract.GetManagingSchemeIds.Call(
            new GetManagingSchemeIdsInput
            {
                Manager = Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName)
            }).SchemeIds;
        State.ElectionContract.SetTreasurySchemeIds.Send(new SetTreasurySchemeIdsInput
        {
            TreasuryHash = schemeIdsManagingByTreasuryContract[0],
            WelcomeHash = schemeIdsManagingByTreasuryContract[3],
            FlexibleHash = schemeIdsManagingByTreasuryContract[4],
            SubsidyHash = schemeIdsManagingByElectionContract[0],
            WelfareHash = schemeIdsManagingByElectionContract[1]
        });
    }

    private void InitializeTokenConverterContract()
    {
        State.TokenConverterContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName);
        var connectors = new List<Connector>
        {
            new()
            {
                Symbol = Context.Variables.NativeSymbol,
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = true,
                Weight = "0.5",
                VirtualBalance = EconomicContractConstants.NativeTokenConnectorInitialVirtualBalance
            }
        };
        foreach (var resourceTokenSymbol in Context.Variables
                     .GetStringArray(EconomicContractConstants.PayTxFeeSymbolListName)
                     .Union(Context.Variables.GetStringArray(EconomicContractConstants.PayRentalSymbolListName)))
        {
            var resourceTokenConnector = new Connector
            {
                Symbol = resourceTokenSymbol,
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = true,
                Weight = "0.005",
                VirtualBalance = EconomicContractConstants.ResourceTokenInitialVirtualBalance,
                RelatedSymbol = EconomicContractConstants.NativeTokenPrefix.Append(resourceTokenSymbol),
                IsDepositAccount = false
            };
            var nativeTokenConnector = new Connector
            {
                Symbol = EconomicContractConstants.NativeTokenPrefix.Append(resourceTokenSymbol),
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = true,
                Weight = "0.005",
                VirtualBalance = EconomicContractConstants.NativeTokenToResourceBalance,
                RelatedSymbol = resourceTokenSymbol,
                IsDepositAccount = true
            };
            connectors.Add(resourceTokenConnector);
            connectors.Add(nativeTokenConnector);
        }

        State.TokenConverterContract.Initialize.Send(new InitializeInput
        {
            FeeRate = EconomicContractConstants.TokenConverterFeeRate,
            Connectors = { connectors },
            BaseTokenSymbol = Context.Variables.NativeSymbol
        });
    }

    private void AssertValidMemo(string memo)
    {
        Assert(Encoding.UTF8.GetByteCount(memo) <= EconomicContractConstants.MemoMaxLength, "Invalid memo size.");
    }
}