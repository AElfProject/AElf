using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenConverter;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS10;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Treasury;

// ReSharper disable InconsistentNaming
/// <summary>
///     The Treasury is the largest profit scheme in AElf main chain.
///     Actually the Treasury is our Dividends Pool.
///     Income of the Treasury is mining rewards
///     (AEDPoS Contract will:
///     1. transfer ELF tokens to general ledger of Treasury every time we change term (7 days),
///     the amount of ELF should be based on blocks produced during last term. 1,000,000 * 1250000 ELF,
///     then release the Treasury;
///     2. Release Treasury)
///     3 sub profit schemes:
///     (Mining Reward for Miners) - 3
///     (Subsidy for Candidates / Backups) - 1
///     (Welfare for Electors / Voters / Citizens) - 1
///     3 sub profit schemes for Mining Rewards:
///     (Basic Rewards) - 4
///     (Welcome Rewards) - 1
///     (Flexible Rewards) - 1
///     3 incomes:
///     1. 20% total supply of elf, from consensus contract
///     2. tx fees.
///     3. resource consumption of developer's contracts.
/// </summary>
public partial class TreasuryContract : TreasuryContractImplContainer.TreasuryContractImplBase
{
    public override Empty InitialTreasuryContract(Empty input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");

        State.ProfitContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);

        // Create profit schemes: `Treasury`, `CitizenWelfare`, `BackupSubsidy`, `MinerReward`,
        // `MinerBasicReward`, `WelcomeReward`, `FlexibleReward`
        var profitItemNameList = new List<string>
        {
            "Treasury", "MinerReward", "Subsidy", "Welfare", "Basic Reward", "Flexible Reward",
            "Welcome Reward"
        };
        for (var i = 0; i < 7; i++)
        {
            var index = i;
            Context.LogDebug(() => profitItemNameList[index]);
            State.ProfitContract.CreateScheme.Send(new CreateSchemeInput
            {
                IsReleaseAllBalanceEveryTimeByDefault = true,
                // Distribution of Citizen Welfare will delay one period.
                DelayDistributePeriodCount = i == 3 ? 1 : 0,
                // Subsidy, Flexible Reward and Welcome Reward can remove beneficiary directly (due to replaceable.)
                CanRemoveBeneficiaryDirectly = new List<int> { 2, 5, 6 }.Contains(i)
            });
        }

        State.Initialized.Value = true;

        State.SymbolList.Value = new SymbolList
        {
            Value = { Context.Variables.NativeSymbol }
        };

        return new Empty();
    }

    public override Empty InitialMiningRewardProfitItem(Empty input)
    {
        Assert(State.TreasuryHash.Value == null, "Already initialized.");
        var managingSchemeIds = State.ProfitContract.GetManagingSchemeIds.Call(new GetManagingSchemeIdsInput
        {
            Manager = Context.Self
        }).SchemeIds;

        Assert(managingSchemeIds.Count == 7, "Incorrect schemes count.");

        State.TreasuryHash.Value = managingSchemeIds[0];
        State.RewardHash.Value = managingSchemeIds[1];
        State.SubsidyHash.Value = managingSchemeIds[2];
        State.WelfareHash.Value = managingSchemeIds[3];
        State.BasicRewardHash.Value = managingSchemeIds[4];
        State.VotesWeightRewardHash.Value = managingSchemeIds[5];
        State.ReElectionRewardHash.Value = managingSchemeIds[6];

        var electionContractAddress =
            Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName);
        if (electionContractAddress != null)
        {
            State.ProfitContract.ResetManager.Send(new ResetManagerInput
            {
                SchemeId = managingSchemeIds[2],
                NewManager = electionContractAddress
            });
            State.ProfitContract.ResetManager.Send(new ResetManagerInput
            {
                SchemeId = managingSchemeIds[3],
                NewManager = electionContractAddress
            });
        }

        BuildTreasury();

        var treasuryVirtualAddress = Address.FromPublicKey(State.ProfitContract.Value.Value.Concat(
            managingSchemeIds[0].Value.ToByteArray().ComputeHash()).ToArray());
        State.TreasuryVirtualAddress.Value = treasuryVirtualAddress;

        return new Empty();
    }

    public override Empty Release(ReleaseInput input)
    {
        RequireAEDPoSContractStateSet();
        Assert(
            Context.Sender == State.AEDPoSContract.Value,
            "Only AElf Consensus Contract can release profits from Treasury.");
        State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
        {
            SchemeId = State.TreasuryHash.Value,
            Period = input.PeriodNumber,
            AmountsMap = { State.SymbolList.Value.Value.ToDictionary(s => s, s => 0L) }
        });
        RequireElectionContractStateSet();
        var previousTermInformation = State.AEDPoSContract.GetPreviousTermInformation.Call(new Int64Value
        {
            Value = input.PeriodNumber
        });

        var currentMinerList = State.AEDPoSContract.GetCurrentMinerList.Call(new Empty()).Pubkeys
            .Select(p => p.ToHex()).ToList();
        var maybeNewElectedMiners = new List<string>();
        maybeNewElectedMiners.AddRange(currentMinerList);
        maybeNewElectedMiners.AddRange(previousTermInformation.RealTimeMinersInformation.Keys);
        var replaceCandidates = State.ReplaceCandidateMap[input.PeriodNumber];
        if (replaceCandidates != null)
        {
            Context.LogDebug(() =>
                $"New miners from replace candidate map: {replaceCandidates.Value.Aggregate((l, r) => $"{l}\n{r}")}");
            maybeNewElectedMiners.AddRange(replaceCandidates.Value);
            State.ReplaceCandidateMap.Remove(input.PeriodNumber);
        }

        maybeNewElectedMiners = maybeNewElectedMiners
            .Where(p => State.LatestMinedTerm[p] == 0 && !GetInitialMinerList().Contains(p)).ToList();
        if (maybeNewElectedMiners.Any())
            Context.LogDebug(() => $"New elected miners: {maybeNewElectedMiners.Aggregate((l, r) => $"{l}\n{r}")}");
        else
            Context.LogDebug(() => "No new elected miner.");

        UpdateStateBeforeDistribution(previousTermInformation, maybeNewElectedMiners);
        ReleaseTreasurySubProfitItems(input.PeriodNumber);
        UpdateStateAfterDistribution(previousTermInformation, currentMinerList);
        return new Empty();
    }

    private List<string> GetInitialMinerList()
    {
        return State.AEDPoSContract.GetRoundInformation.Call(new Int64Value { Value = 1 }).RealTimeMinersInformation
            .Keys.ToList();
    }

    public override Empty Donate(DonateInput input)
    {
        Assert(input.Amount > 0, "Invalid amount of donating. Amount needs to be greater than 0.");
        if (State.TokenContract.Value == null)
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

        if (!State.TokenContract.IsTokenAvailableForMethodFee.Call(new StringValue { Value = input.Symbol }).Value)
            return new Empty();

        if (State.TokenConverterContract.Value == null)
            State.TokenConverterContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName);

        var isNativeSymbol = input.Symbol == Context.Variables.NativeSymbol;
        var canExchangeWithNativeSymbol =
            isNativeSymbol ||
            State.TokenConverterContract.IsSymbolAbleToSell
                .Call(new StringValue { Value = input.Symbol }).Value;

        if (Context.Sender != Context.Self)
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = input.Symbol,
                Amount = input.Amount,
                Memo = "Donate to treasury."
            });

        var needToConvert = !isNativeSymbol && canExchangeWithNativeSymbol;
        if (needToConvert)
        {
            ConvertToNativeToken(input.Symbol, input.Amount);
        }
        else
        {
            State.TokenContract.Approve.Send(new ApproveInput
            {
                Symbol = input.Symbol,
                Amount = input.Amount,
                Spender = State.ProfitContract.Value
            });

            State.ProfitContract.ContributeProfits.Send(new ContributeProfitsInput
            {
                SchemeId = State.TreasuryHash.Value,
                Symbol = input.Symbol,
                Amount = input.Amount
            });

            var donatesOfCurrentBlock = State.DonatedDividends[Context.CurrentHeight];
            if (donatesOfCurrentBlock != null && Context.Variables.NativeSymbol == input.Symbol &&
                donatesOfCurrentBlock.Value.ContainsKey(Context.Variables.NativeSymbol))
                donatesOfCurrentBlock.Value[Context.Variables.NativeSymbol] = donatesOfCurrentBlock
                    .Value[Context.Variables.NativeSymbol].Add(input.Amount);
            else
                donatesOfCurrentBlock = new Dividends
                {
                    Value =
                    {
                        { input.Symbol, input.Amount }
                    }
                };

            State.DonatedDividends[Context.CurrentHeight] = donatesOfCurrentBlock;

            Context.Fire(new DonationReceived
            {
                From = Context.Sender,
                Symbol = input.Symbol,
                Amount = input.Amount,
                PoolContract = Context.Self
            });
        }

        return new Empty();
    }

    public override Empty DonateAll(DonateAllInput input)
    {
        if (State.TokenContract.Value == null)
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

        var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
        {
            Symbol = input.Symbol,
            Owner = Context.Sender
        }).Balance;

        Donate(new DonateInput
        {
            Symbol = input.Symbol,
            Amount = balance
        });

        return new Empty();
    }

    public override Empty ChangeTreasuryController(AuthorityInfo input)
    {
        AssertPerformedByTreasuryController();
        Assert(CheckOrganizationExist(input), "Invalid authority input.");
        State.TreasuryController.Value = input;
        return new Empty();
    }

    public override Empty SetSymbolList(SymbolList input)
    {
        AssertPerformedByTreasuryController();
        Assert(input.Value.Contains(Context.Variables.NativeSymbol), "Need to contain native symbol.");
        if (State.TokenContract.Value == null)
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

        if (State.TokenConverterContract.Value == null)
            State.TokenConverterContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName);

        foreach (var symbol in input.Value.Where(s => s != Context.Variables.NativeSymbol))
        {
            var isTreasuryInWhiteList = State.TokenContract.IsInWhiteList.Call(new IsInWhiteListInput
            {
                Symbol = symbol,
                Address = Context.Self
            }).Value;
            Assert(
                State.TokenContract.IsTokenAvailableForMethodFee.Call(new StringValue { Value = symbol }).Value ||
                isTreasuryInWhiteList, "Symbol need to be profitable.");
            Assert(!State.TokenConverterContract.IsSymbolAbleToSell.Call(new StringValue { Value = symbol }).Value,
                $"Token {symbol} doesn't need to set to symbol list because it would become native token after donation.");
        }

        State.SymbolList.Value = input;
        return new Empty();
    }

    public override Empty SetDividendPoolWeightSetting(DividendPoolWeightSetting input)
    {
        AssertPerformedByTreasuryController();
        Assert(
            input.CitizenWelfareWeight > 0 && input.BackupSubsidyWeight > 0 &&
            input.MinerRewardWeight > 0,
            "invalid input");
        ResetSubSchemeToTreasury(input);
        State.DividendPoolWeightSetting.Value = input;
        return new Empty();
    }

    public override Empty SetMinerRewardWeightSetting(MinerRewardWeightSetting input)
    {
        AssertPerformedByTreasuryController();
        Assert(
            input.BasicMinerRewardWeight > 0 && input.WelcomeRewardWeight > 0 &&
            input.FlexibleRewardWeight > 0,
            "invalid input");
        ResetSubSchemeToMinerReward(input);
        State.MinerRewardWeightSetting.Value = input;
        return new Empty();
    }

    public override GetWelfareRewardAmountSampleOutput GetWelfareRewardAmountSample(
        GetWelfareRewardAmountSampleInput input)
    {
        const long sampleAmount = 10000;
        var welfareHash = State.WelfareHash.Value;
        var output = new GetWelfareRewardAmountSampleOutput();
        var welfareScheme = State.ProfitContract.GetScheme.Call(welfareHash);
        var releasedInformation = State.ProfitContract.GetDistributedProfitsInfo.Call(
            new SchemePeriod
            {
                SchemeId = welfareHash,
                Period = welfareScheme.CurrentPeriod.Sub(1)
            });
        var totalShares = releasedInformation.TotalShares;
        if (totalShares == 0) return new GetWelfareRewardAmountSampleOutput();

        var totalAmount = releasedInformation.AmountsMap;
        foreach (var lockTime in input.Value)
        {
            var shares = GetVotesWeight(sampleAmount, lockTime);
            // In case of arithmetic overflow
            var decimalAmount = (decimal)totalAmount[Context.Variables.NativeSymbol];
            var decimalShares = (decimal)shares;
            var decimalTotalShares = (decimal)totalShares;
            var amount = decimalAmount * decimalShares / decimalTotalShares;
            output.Value.Add((long)amount);
        }

        return output;
    }

    public override Dividends GetUndistributedDividends(Empty input)
    {
        return new Dividends
        {
            Value =
            {
                State.SymbolList.Value.Value.Select(s => State.TokenContract.GetBalance.Call(new GetBalanceInput
                {
                    Owner = State.TreasuryVirtualAddress.Value,
                    Symbol = s
                })).ToDictionary(b => b.Symbol, b => b.Balance)
            }
        };
    }

    public override Hash GetTreasurySchemeId(Empty input)
    {
        return State.TreasuryHash.Value ?? Hash.Empty;
    }

    public override AuthorityInfo GetTreasuryController(Empty input)
    {
        if (State.TreasuryController.Value == null) return GetDefaultTreasuryController();

        return State.TreasuryController.Value;
    }

    public override SymbolList GetSymbolList(Empty input)
    {
        return State.SymbolList.Value;
    }

    public override MinerRewardWeightProportion GetMinerRewardWeightProportion(Empty input)
    {
        var weightSetting = State.MinerRewardWeightSetting.Value ?? GetDefaultMinerRewardWeightSetting();
        var weightSum = weightSetting.BasicMinerRewardWeight.Add(weightSetting.WelcomeRewardWeight)
            .Add(weightSetting.FlexibleRewardWeight);
        var weightProportion = new MinerRewardWeightProportion
        {
            BasicMinerRewardProportionInfo = new SchemeProportionInfo
            {
                SchemeId = State.BasicRewardHash.Value,
                Proportion = weightSetting.BasicMinerRewardWeight
                    .Mul(TreasuryContractConstants.OneHundredPercent).Div(weightSum)
            },
            WelcomeRewardProportionInfo = new SchemeProportionInfo
            {
                SchemeId = State.VotesWeightRewardHash.Value,
                Proportion = weightSetting.WelcomeRewardWeight
                    .Mul(TreasuryContractConstants.OneHundredPercent).Div(weightSum)
            }
        };
        weightProportion.FlexibleRewardProportionInfo = new SchemeProportionInfo
        {
            SchemeId = State.ReElectionRewardHash.Value,
            Proportion = TreasuryContractConstants.OneHundredPercent
                .Sub(weightProportion.BasicMinerRewardProportionInfo.Proportion)
                .Sub(weightProportion.WelcomeRewardProportionInfo.Proportion)
        };
        return weightProportion;
    }

    public override DividendPoolWeightProportion GetDividendPoolWeightProportion(Empty input)
    {
        var weightSetting = State.DividendPoolWeightSetting.Value ?? GetDefaultDividendPoolWeightSetting();
        var weightSum = weightSetting.BackupSubsidyWeight.Add(weightSetting.CitizenWelfareWeight)
            .Add(weightSetting.MinerRewardWeight);
        var weightProportion = new DividendPoolWeightProportion
        {
            BackupSubsidyProportionInfo = new SchemeProportionInfo
            {
                SchemeId = State.SubsidyHash.Value,
                Proportion = weightSetting.BackupSubsidyWeight
                    .Mul(TreasuryContractConstants.OneHundredPercent).Div(weightSum)
            },
            CitizenWelfareProportionInfo = new SchemeProportionInfo
            {
                SchemeId = State.WelfareHash.Value,
                Proportion = weightSetting.CitizenWelfareWeight
                    .Mul(TreasuryContractConstants.OneHundredPercent).Div(weightSum)
            }
        };
        weightProportion.MinerRewardProportionInfo = new SchemeProportionInfo
        {
            SchemeId = State.RewardHash.Value,
            Proportion = TreasuryContractConstants.OneHundredPercent
                .Sub(weightProportion.BackupSubsidyProportionInfo.Proportion)
                .Sub(weightProportion.CitizenWelfareProportionInfo.Proportion)
        };
        return weightProportion;
    }

    private long GetVotesWeight(long votesAmount, long lockTime)
    {
        RequireElectionContractStateSet();
        var weight = State.ElectionContract.GetCalculateVoteWeight.Call(new VoteInformation
        {
            Amount = votesAmount,
            LockTime = lockTime
        });
        return weight.Value;
    }

    private DividendPoolWeightSetting GetDefaultDividendPoolWeightSetting()
    {
        return new DividendPoolWeightSetting
        {
            CitizenWelfareWeight = 15,
            BackupSubsidyWeight = 1,
            MinerRewardWeight = 4
        };
    }

    private MinerRewardWeightSetting GetDefaultMinerRewardWeightSetting()
    {
        return new MinerRewardWeightSetting
        {
            BasicMinerRewardWeight = 2,
            WelcomeRewardWeight = 1,
            FlexibleRewardWeight = 1
        };
    }

    private void ResetSubSchemeToTreasury(DividendPoolWeightSetting newWeightSetting)
    {
        var oldWeightSetting = State.DividendPoolWeightSetting.Value ?? new DividendPoolWeightSetting();
        var parentSchemeId = State.TreasuryHash.Value;
        // Register or reset `MinerReward` to `Treasury`
        ResetWeight(parentSchemeId, State.RewardHash.Value,
            oldWeightSetting.MinerRewardWeight, newWeightSetting.MinerRewardWeight);
        // Register or reset `BackupSubsidy` to `Treasury`
        ResetWeight(parentSchemeId, State.SubsidyHash.Value,
            oldWeightSetting.BackupSubsidyWeight, newWeightSetting.BackupSubsidyWeight);
        // Register or reset `CitizenWelfare` to `Treasury`
        ResetWeight(parentSchemeId, State.WelfareHash.Value,
            oldWeightSetting.CitizenWelfareWeight, newWeightSetting.CitizenWelfareWeight);
    }

    private void ResetSubSchemeToMinerReward(MinerRewardWeightSetting newWeightSetting)
    {
        var oldWeightSetting = State.MinerRewardWeightSetting.Value ?? new MinerRewardWeightSetting();
        var parentSchemeId = State.RewardHash.Value;
        // Register or reset `MinerBasicReward` to `MinerReward`
        ResetWeight(parentSchemeId, State.BasicRewardHash.Value,
            oldWeightSetting.BasicMinerRewardWeight, newWeightSetting.BasicMinerRewardWeight);
        // Register or reset `WelcomeRewardWeight` to `MinerReward`
        ResetWeight(parentSchemeId, State.VotesWeightRewardHash.Value,
            oldWeightSetting.WelcomeRewardWeight, newWeightSetting.WelcomeRewardWeight);
        // Register or reset `FlexibleRewardWeight` to `MinerReward`
        ResetWeight(parentSchemeId, State.ReElectionRewardHash.Value,
            oldWeightSetting.FlexibleRewardWeight, newWeightSetting.FlexibleRewardWeight);
    }

    private void ResetWeight(Hash parentSchemeId, Hash subSchemeId, int oldWeight,
        int newWeight)
    {
        if (oldWeight == newWeight)
            return;

        // old weight equals 0 indicates the subScheme has not been registered
        if (oldWeight > 0)
            State.ProfitContract.RemoveSubScheme.Send(new RemoveSubSchemeInput
            {
                SchemeId = parentSchemeId,
                SubSchemeId = subSchemeId
            });

        State.ProfitContract.AddSubScheme.Send(new AddSubSchemeInput
        {
            SchemeId = parentSchemeId,
            SubSchemeId = subSchemeId,
            SubSchemeShares = newWeight
        });
    }

    public override Empty UpdateMiningReward(Int64Value input)
    {
        Assert(Context.Sender ==
               Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName),
            "Only consensus contract can update mining reward.");
        State.MiningReward.Value = input.Value;
        return new Empty();
    }

    public override Dividends GetDividends(Int64Value input)
    {
        Assert(Context.CurrentHeight > input.Value, "Cannot query dividends of a future block.");
        var dividends = State.DonatedDividends[input.Value];

        if (dividends != null && dividends.Value.ContainsKey(Context.Variables.NativeSymbol))
            dividends.Value[Context.Variables.NativeSymbol] =
                dividends.Value[Context.Variables.NativeSymbol].Add(State.MiningReward.Value);
        else
            dividends = new Dividends
            {
                Value =
                {
                    {
                        Context.Variables.NativeSymbol, State.MiningReward.Value
                    }
                }
            };

        return dividends;
    }

    public override Empty RecordMinerReplacement(RecordMinerReplacementInput input)
    {
        Assert(
            Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName) == Context.Sender,
            "Only AEDPoS Contract can record miner replacement.");

        if (State.ProfitContract.Value == null)
            State.ProfitContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);

        if (!input.IsOldPubkeyEvil)
        {
            var latestMinedTerm = State.LatestMinedTerm[input.OldPubkey];
            State.LatestMinedTerm[input.NewPubkey] = latestMinedTerm;
            State.LatestMinedTerm.Remove(input.OldPubkey);
        }
        else
        {
            var replaceCandidates = State.ReplaceCandidateMap[input.CurrentTermNumber] ?? new StringList();
            replaceCandidates.Value.Add(input.NewPubkey);
            State.ReplaceCandidateMap[input.CurrentTermNumber] = replaceCandidates;
        }

        State.IsReplacedEvilMiner[input.NewPubkey] = true;

        return new Empty();
    }

    public override Empty SetProfitsReceiver(SetProfitsReceiverInput input)
    {
        if (State.ElectionContract.Value == null)
            State.ElectionContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName);
        var pubkey = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(input.Pubkey));
        
        var admin = State.ElectionContract.GetCandidateAdmin.Call(new StringValue {Value = input.Pubkey});
        Assert(Context.Origin == admin , "No permission.");
        
        var candidateList = State.ElectionContract.GetCandidates.Call(new Empty());
        Assert(candidateList.Value.Contains(pubkey),"Pubkey is not a candidate.");

        var previousProfitsReceiver = State.ProfitsReceiverMap[input.Pubkey];
        //Set same profits receiver address.
        if (input.ProfitsReceiverAddress == previousProfitsReceiver)
        {
            return new Empty();
        }
        State.ProfitsReceiverMap[input.Pubkey] = input.ProfitsReceiverAddress;
        State.ElectionContract.SetProfitsReceiver.Send(new AElf.Contracts.Election.SetProfitsReceiverInput
        {
            CandidatePubkey = input.Pubkey,
            ReceiverAddress = input.ProfitsReceiverAddress,
            PreviousReceiverAddress = previousProfitsReceiver ?? new Address()
        });

        return new Empty();
    }

    public override Empty ReplaceCandidateProfitsReceiver(ReplaceCandidateProfitsReceiverInput input)
    {
        Assert(Context.Sender == Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName),
            "No permission");
        var profitReceiver = State.ProfitsReceiverMap[input.OldPubkey];
        State.ProfitsReceiverMap.Remove(input.OldPubkey);
        State.ProfitsReceiverMap[input.NewPubkey] = profitReceiver;
        return new Empty();
    }

    public override Address GetProfitsReceiver(StringValue input)
    {
        return GetProfitsReceiver(input.Value);
    }

    public override Address GetProfitsReceiverOrDefault(StringValue input)
    {
        return State.ProfitsReceiverMap[input.Value];
    }

    private Address GetProfitsReceiver(string pubkey)
    {
        return State.ProfitsReceiverMap[pubkey] ??
               Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(pubkey));
    }

    private List<Address> GetAddressesFromCandidatePubkeys(ICollection<string> pubkeys)
    {
        var addresses = pubkeys.Select(k => Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(k)))
            .ToList();
        addresses.AddRange(pubkeys.Select(GetProfitsReceiver));
        return addresses;
    }

    #region Private methods

    private void ConvertToNativeToken(string symbol, long amount)
    {
        State.TokenContract.Approve.Send(new ApproveInput
        {
            Spender = State.TokenConverterContract.Value,
            Symbol = symbol,
            Amount = amount
        });

        State.TokenConverterContract.Sell.Send(new SellInput
        {
            Symbol = symbol,
            Amount = amount
        });

        Context.SendInline(Context.Self, nameof(DonateAll), new DonateAllInput
        {
            Symbol = Context.Variables.NativeSymbol
        });
    }

    private void BuildTreasury()
    {
        if (State.DividendPoolWeightSetting.Value == null)
        {
            var dividendPoolWeightSetting = GetDefaultDividendPoolWeightSetting();
            ResetSubSchemeToTreasury(dividendPoolWeightSetting);
            State.DividendPoolWeightSetting.Value = dividendPoolWeightSetting;
        }

        if (State.MinerRewardWeightSetting.Value == null)
        {
            var minerRewardWeightSetting = GetDefaultMinerRewardWeightSetting();
            ResetSubSchemeToMinerReward(minerRewardWeightSetting);
            State.MinerRewardWeightSetting.Value = minerRewardWeightSetting;
        }
    }

    private void ReleaseTreasurySubProfitItems(long termNumber)
    {
        var amountsMap = State.SymbolList.Value.Value.ToDictionary(s => s, s => 0L);
        State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
        {
            SchemeId = State.RewardHash.Value,
            Period = termNumber,
            AmountsMap = { amountsMap }
        });

        State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
        {
            SchemeId = State.VotesWeightRewardHash.Value,
            Period = termNumber,
            AmountsMap = { amountsMap }
        });

        State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
        {
            SchemeId = State.ReElectionRewardHash.Value,
            Period = termNumber,
            AmountsMap = { amountsMap }
        });

        State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
        {
            SchemeId = State.BasicRewardHash.Value,
            Period = termNumber,
            AmountsMap = { amountsMap }
        });
    }

    private void RequireAEDPoSContractStateSet()
    {
        if (State.AEDPoSContract.Value == null)
            State.AEDPoSContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
    }

    private void RequireElectionContractStateSet()
    {
        if (State.ElectionContract.Value == null)
            State.ElectionContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName);
    }

    private void UpdateStateBeforeDistribution(Round previousTermInformation, List<string> newElectedMiners)
    {
        var previousPreviousTermInformation = State.AEDPoSContract.GetPreviousTermInformation.Call(new Int64Value
        {
            Value = previousTermInformation.TermNumber.Sub(1)
        });

        if (newElectedMiners.Any()) State.HasNewMiner[previousTermInformation.TermNumber.Add(1)] = true;

        Context.LogDebug(() => $"Will update weights after term {previousTermInformation.TermNumber}");
        UpdateBasicMinerRewardWeights(new List<Round> { previousPreviousTermInformation, previousTermInformation });
        UpdateWelcomeRewardWeights(previousTermInformation, newElectedMiners);
        UpdateFlexibleRewardWeights(previousTermInformation);
    }

    private void UpdateStateAfterDistribution(Round previousTermInformation, List<string> currentMinerList)
    {
        foreach (var miner in currentMinerList) State.LatestMinedTerm[miner] = previousTermInformation.TermNumber;
    }

    /// <summary>
    ///     Remove current total shares of Basic Reward,
    ///     Add new shares for miners of next term.
    ///     1 share for each miner.
    /// </summary>
    /// <param name="previousTermInformation"></param>
    private void UpdateBasicMinerRewardWeights(IReadOnlyCollection<Round> previousTermInformation)
    {
        if (previousTermInformation.First().RealTimeMinersInformation != null)
            State.ProfitContract.RemoveBeneficiaries.Send(new RemoveBeneficiariesInput
            {
                SchemeId = State.BasicRewardHash.Value,
                Beneficiaries =
                {
                    GetAddressesFromCandidatePubkeys(previousTermInformation.First().RealTimeMinersInformation.Keys)
                }
            });

        var averageProducedBlocksCount = CalculateAverage(previousTermInformation.Last().RealTimeMinersInformation
            .Values
            .Select(i => i.ProducedBlocks).ToList());
        // Manage weights of `MinerBasicReward`
        State.ProfitContract.AddBeneficiaries.Send(new AddBeneficiariesInput
        {
            SchemeId = State.BasicRewardHash.Value,
            EndPeriod = previousTermInformation.Last().TermNumber,
            BeneficiaryShares =
            {
                previousTermInformation.Last().RealTimeMinersInformation.Values.Select(i =>
                {
                    long shares;
                    if (State.IsReplacedEvilMiner[i.Pubkey])
                    {
                        // The new miner may have more shares than his actually contributes, but it's ok.
                        shares = i.ProducedBlocks;
                        // Clear the state asap.
                        State.IsReplacedEvilMiner.Remove(i.Pubkey);
                    }
                    else
                    {
                        shares = CalculateShares(i.ProducedBlocks, averageProducedBlocksCount);
                    }

                    return new BeneficiaryShare
                    {
                        Beneficiary = GetProfitsReceiver(i.Pubkey),
                        Shares = shares
                    };
                })
            }
        });
    }

    /// <summary>
    ///     Just to make sure not using double type.
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    private long CalculateAverage(List<long> list)
    {
        var sum = list.Sum();
        return sum.Div(list.Count);
    }

    private long CalculateShares(long producedBlocksCount, long averageProducedBlocksCount)
    {
        if (producedBlocksCount < averageProducedBlocksCount.Div(2))
            // If count < (1/2) * average_count, then this node won't share Basic Miner Reward.
            return 0;

        if (producedBlocksCount < averageProducedBlocksCount.Div(5).Mul(4))
            // If count < (4/5) * average_count, then ratio will be (count / average_count)
            return producedBlocksCount.Mul(producedBlocksCount).Div(averageProducedBlocksCount);

        return producedBlocksCount;
    }

    private void UpdateWelcomeRewardWeights(Round previousTermInformation, List<string> newElectedMiners)
    {
        var previousMinerAddresses =
            GetAddressesFromCandidatePubkeys(previousTermInformation.RealTimeMinersInformation.Keys);
        var possibleWelcomeBeneficiaries = new RemoveBeneficiariesInput
        {
            SchemeId = State.VotesWeightRewardHash.Value,
            Beneficiaries = { previousMinerAddresses }
        };
        State.ProfitContract.RemoveBeneficiaries.Send(possibleWelcomeBeneficiaries);
        State.ProfitContract.RemoveSubScheme.Send(new RemoveSubSchemeInput
        {
            SchemeId = State.VotesWeightRewardHash.Value,
            SubSchemeId = State.BasicRewardHash.Value
        });

        if (newElectedMiners.Any())
        {
            Context.LogDebug(() => "Welcome reward will go to new miners.");
            var newBeneficiaries = new AddBeneficiariesInput
            {
                SchemeId = State.VotesWeightRewardHash.Value,
                EndPeriod = previousTermInformation.TermNumber.Add(1)
            };
            foreach (var minerAddress in newElectedMiners.Select(GetProfitsReceiver))
                newBeneficiaries.BeneficiaryShares.Add(new BeneficiaryShare
                {
                    Beneficiary = minerAddress,
                    Shares = 1
                });

            if (newBeneficiaries.BeneficiaryShares.Any()) State.ProfitContract.AddBeneficiaries.Send(newBeneficiaries);
        }
        else
        {
            Context.LogDebug(() => "Welcome reward will go to Basic Reward.");
            State.ProfitContract.AddSubScheme.Send(new AddSubSchemeInput
            {
                SchemeId = State.VotesWeightRewardHash.Value,
                SubSchemeId = State.BasicRewardHash.Value,
                SubSchemeShares = 1
            });
        }
    }

    private void UpdateFlexibleRewardWeights(Round previousTermInformation)
    {
        State.ProfitContract.RemoveSubScheme.Send(new RemoveSubSchemeInput
        {
            SchemeId = State.ReElectionRewardHash.Value,
            SubSchemeId = State.WelfareHash.Value
        });
        State.ProfitContract.RemoveSubScheme.Send(new RemoveSubSchemeInput
        {
            SchemeId = State.ReElectionRewardHash.Value,
            SubSchemeId = State.BasicRewardHash.Value
        });
        if (State.ProfitContract.GetScheme.Call(State.ReElectionRewardHash.Value).TotalShares > 0)
        {
            var previousMinerAddresses =
                GetAddressesFromCandidatePubkeys(previousTermInformation.RealTimeMinersInformation.Keys);
            State.ProfitContract.RemoveBeneficiaries.Send(new RemoveBeneficiariesInput
            {
                SchemeId = State.ReElectionRewardHash.Value,
                Beneficiaries = { previousMinerAddresses }
            });
        }

        if (State.HasNewMiner[previousTermInformation.TermNumber])
        {
            Context.LogDebug(() => "Flexible reward will go to Welfare Reward.");
            State.ProfitContract.AddSubScheme.Send(new AddSubSchemeInput
            {
                SchemeId = State.ReElectionRewardHash.Value,
                SubSchemeId = State.WelfareHash.Value,
                SubSchemeShares = 1
            });
        }
        else
        {
            Context.LogDebug(() => "Flexible reward will go to Basic Reward.");
            State.ProfitContract.AddSubScheme.Send(new AddSubSchemeInput
            {
                SchemeId = State.ReElectionRewardHash.Value,
                SubSchemeId = State.BasicRewardHash.Value,
                SubSchemeShares = 1
            });
        }
    }

    private void AssertPerformedByTreasuryController()
    {
        if (State.TreasuryController.Value == null) State.TreasuryController.Value = GetDefaultTreasuryController();

        Assert(Context.Sender == State.TreasuryController.Value.OwnerAddress, "no permission");
    }

    private AuthorityInfo GetDefaultTreasuryController()
    {
        if (State.ParliamentContract.Value == null)
            State.ParliamentContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);

        return new AuthorityInfo
        {
            ContractAddress = State.ParliamentContract.Value,
            OwnerAddress = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty())
        };
    }

    #endregion
}