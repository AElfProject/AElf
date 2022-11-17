using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Profit;

/// <summary>
///     Let's imagine a scenario:
///     1. Ean creates a profit scheme FOO: Ean calls CreateScheme. We call this profit scheme PI_FOO.
///     2. GL creates another profit scheme BAR: GL calls CreateScheme. We call this profit scheme PI_BAR.
///     3. Ean (as the creator of PI_FOO) register PI_BAR as a sub profit scheme as PI_FOO:
///     Ean call RemoveSubScheme (SchemeId: PI_BAR's profit id, Shares : 1)
///     4. Anil has an account which address is ADDR_Anil.
///     5. Ean registers address ADDR_Anil as a profit Beneficiary of PI_FOO: Ean calls AddBeneficiary (Beneficiary:
///     ADDR_Anil, Shares : 1)
///     6: Now PI_FOO is organized like this:
///     PI_FOO
///     /      \
///     1        1
///     /          \
///     PI_BAR     ADDR_Anil
///     (Total Shares is 2)
///     7. Ean adds some ELF tokens to PI_FOO: Ean calls DistributeProfits (Symbol: "ELF", Amount: 1000L, Period: 1)
///     8. Ean calls DistributeProfits: Balance of PI_BAR is 500L (PI_BAR's general ledger balance, also we can say balance
///     of virtual address of PI_BAR is 500L),
///     9. Balance of PI_FOO's virtual address of first period is 500L.
///     10. Anil can only get his profits by calling Profit (SchemeId: PI_BAR's profit id, Symbol: "ELF")
/// </summary>
public partial class ProfitContract : ProfitContractImplContainer.ProfitContractImplBase
{
    /// <summary>
    ///     Create a Scheme of profit distribution.
    ///     At the first time, the scheme's id is unknown,it may create by transaction id and createdSchemeIds;
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Hash CreateScheme(CreateSchemeInput input)
    {
        ValidateContractState(State.TokenContract, SmartContractConstants.TokenContractSystemName);

        if (input.ProfitReceivingDuePeriodCount == 0)
            input.ProfitReceivingDuePeriodCount = ProfitContractConstants.DefaultProfitReceivingDuePeriodCount;
        else
            Assert(
                input.ProfitReceivingDuePeriodCount > 0 &&
                input.ProfitReceivingDuePeriodCount <= ProfitContractConstants.MaximumProfitReceivingDuePeriodCount,
                "Invalid profit receiving due period count.");

        var schemeId = GenerateSchemeId(input);
        var manager = input.Manager ?? Context.Sender;
        var scheme = GetNewScheme(input, schemeId, manager);
        Assert(State.SchemeInfos[schemeId] == null, "Already exists.");
        State.SchemeInfos[schemeId] = scheme;

        var schemeIds = State.ManagingSchemeIds[scheme.Manager];
        if (schemeIds == null)
            schemeIds = new CreatedSchemeIds
            {
                SchemeIds = { schemeId }
            };
        else
            schemeIds.SchemeIds.Add(schemeId);

        State.ManagingSchemeIds[scheme.Manager] = schemeIds;

        Context.LogDebug(() => $"Created scheme {State.SchemeInfos[schemeId]}");

        Context.Fire(new SchemeCreated
        {
            SchemeId = scheme.SchemeId,
            Manager = scheme.Manager,
            IsReleaseAllBalanceEveryTimeByDefault = scheme.IsReleaseAllBalanceEveryTimeByDefault,
            ProfitReceivingDuePeriodCount = scheme.ProfitReceivingDuePeriodCount,
            VirtualAddress = scheme.VirtualAddress
        });
        return schemeId;
    }

    /// <summary>
    ///     Add a child to a existed scheme.
    /// </summary>
    /// <param name="input">AddSubSchemeInput</param>
    /// <returns></returns>
    public override Empty AddSubScheme(AddSubSchemeInput input)
    {
        Assert(input.SchemeId != input.SubSchemeId, "Two schemes cannot be same.");
        Assert(input.SubSchemeShares > 0, "Shares of sub scheme should greater than 0.");

        var scheme = State.SchemeInfos[input.SchemeId];
        Assert(scheme != null, "Scheme not found.");
        // ReSharper disable once PossibleNullReferenceException
        Assert(Context.Sender == scheme.Manager, "Only manager can add sub-scheme.");
        Assert(scheme.SubSchemes.All(s => s.SchemeId != input.SubSchemeId),
            $"Sub scheme {input.SubSchemeId} already exist.");

        var subSchemeId = input.SubSchemeId;
        var subScheme = State.SchemeInfos[subSchemeId];
        Assert(subScheme != null, "Sub scheme not found.");

        var subSchemeVirtualAddress = Context.ConvertVirtualAddressToContractAddress(subSchemeId);
        // Add profit details and total shares of the father scheme.
        AddBeneficiary(new AddBeneficiaryInput
        {
            SchemeId = input.SchemeId,
            BeneficiaryShare = new BeneficiaryShare
            {
                Beneficiary = subSchemeVirtualAddress,
                Shares = input.SubSchemeShares
            },
            EndPeriod = long.MaxValue
        });

        // Add a sub profit scheme.
        scheme.SubSchemes.Add(new SchemeBeneficiaryShare
        {
            SchemeId = input.SubSchemeId,
            Shares = input.SubSchemeShares
        });
        State.SchemeInfos[input.SchemeId] = scheme;

        return new Empty();
    }

    public override Empty RemoveSubScheme(RemoveSubSchemeInput input)
    {
        Assert(input.SchemeId != input.SubSchemeId, "Two schemes cannot be same.");

        var scheme = State.SchemeInfos[input.SchemeId];
        Assert(scheme != null, "Scheme not found.");

        // ReSharper disable once PossibleNullReferenceException
        Assert(Context.Sender == scheme.Manager, "Only manager can remove sub-scheme.");

        var shares = scheme.SubSchemes.SingleOrDefault(d => d.SchemeId == input.SubSchemeId);
        if (shares == null) return new Empty();

        var subSchemeId = input.SubSchemeId;
        var subScheme = State.SchemeInfos[subSchemeId];
        Assert(subScheme != null, "Sub scheme not found.");

        var subSchemeVirtualAddress = Context.ConvertVirtualAddressToContractAddress(subSchemeId);
        // Remove profit details
        State.ProfitDetailsMap[input.SchemeId][subSchemeVirtualAddress] = new ProfitDetails();
        scheme.SubSchemes.Remove(shares);
        scheme.TotalShares = scheme.TotalShares.Sub(shares.Shares);
        State.SchemeInfos[input.SchemeId] = scheme;

        return new Empty();
    }

    public override Empty AddBeneficiary(AddBeneficiaryInput input)
    {
        AssertValidInput(input);
        if (input.EndPeriod == 0)
            // Which means this profit Beneficiary will never expired unless removed.
            input.EndPeriod = long.MaxValue;

        var schemeId = input.SchemeId;
        var scheme = State.SchemeInfos[schemeId];

        Assert(scheme != null, "Scheme not found.");

        // ReSharper disable once PossibleNullReferenceException
        Assert(
            Context.Sender == scheme.Manager || Context.Sender ==
            Context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName),
            "Only manager can add beneficiary.");

        Context.LogDebug(() =>
            $"{input.SchemeId}.\n End Period: {input.EndPeriod}, Current Period: {scheme.CurrentPeriod}");

        Assert(input.EndPeriod >= scheme.CurrentPeriod,
            $"Invalid end period. End Period: {input.EndPeriod}, Current Period: {scheme.CurrentPeriod}");

        scheme.TotalShares = scheme.TotalShares.Add(input.BeneficiaryShare.Shares);

        State.SchemeInfos[schemeId] = scheme;

        var profitDetail = new ProfitDetail
        {
            StartPeriod = scheme.CurrentPeriod.Add(scheme.DelayDistributePeriodCount),
            EndPeriod = input.EndPeriod,
            Shares = input.BeneficiaryShare.Shares,
            Id = input.ProfitDetailId
        };

        var currentProfitDetails = State.ProfitDetailsMap[schemeId][input.BeneficiaryShare.Beneficiary];
        if (currentProfitDetails == null)
            currentProfitDetails = new ProfitDetails
            {
                Details = { profitDetail }
            };
        else
            currentProfitDetails.Details.Add(profitDetail);

        // Remove details too old.
        var oldProfitDetails = currentProfitDetails.Details.Where(
            d => d.EndPeriod != long.MaxValue && d.LastProfitPeriod >= d.EndPeriod &&
                 d.EndPeriod.Add(scheme.ProfitReceivingDuePeriodCount) < scheme.CurrentPeriod).ToList();
        foreach (var detail in oldProfitDetails) currentProfitDetails.Details.Remove(detail);

        State.ProfitDetailsMap[schemeId][input.BeneficiaryShare.Beneficiary] = currentProfitDetails;

        Context.LogDebug(() =>
            $"Added {input.BeneficiaryShare.Shares} weights to scheme {input.SchemeId.ToHex()}: {profitDetail}");

        return new Empty();
    }

    private void AssertValidInput(AddBeneficiaryInput input)
    {
        Assert(input.SchemeId != null, "Invalid scheme id.");
        Assert(input.BeneficiaryShare?.Beneficiary != null, "Invalid beneficiary address.");
        Assert(input.BeneficiaryShare?.Shares >= 0, "Invalid share.");
    }

    public override Empty RemoveBeneficiary(RemoveBeneficiaryInput input)
    {
        Assert(input.SchemeId != null, "Invalid scheme id.");
        Assert(input.Beneficiary != null, "Invalid Beneficiary address.");

        var scheme = State.SchemeInfos[input.SchemeId];

        Assert(scheme != null, "Scheme not found.");

        var currentDetail = State.ProfitDetailsMap[input.SchemeId][input.Beneficiary];

        if (scheme == null || currentDetail == null) return new Empty();

        Assert(Context.Sender == scheme.Manager || Context.Sender ==
            Context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName),
            "Only manager or token holder contract can add beneficiary.");

        var removedDetails = RemoveProfitDetails(scheme, input.Beneficiary, input.ProfitDetailId);

        foreach (var (removedMinPeriod, removedShares) in removedDetails.Where(d => d.Key != 0))
        {
            if (scheme.DelayDistributePeriodCount > 0)
            {
                for (var removedPeriod = removedMinPeriod;
                     removedPeriod < removedMinPeriod.Add(scheme.DelayDistributePeriodCount);
                     removedPeriod++)
                {
                    if (scheme.CachedDelayTotalShares.ContainsKey(removedPeriod))
                    {
                        scheme.CachedDelayTotalShares[removedPeriod] =
                            scheme.CachedDelayTotalShares[removedPeriod].Sub(removedShares);
                    }
                }
            }
        }

        State.SchemeInfos[input.SchemeId].TotalShares = scheme.TotalShares.Sub(removedDetails.Values.Sum());

        return new Empty();
    }

    public override Empty FixProfitDetail(FixProfitDetailInput input)
    {
        Assert(input.SchemeId != null, "Invalid scheme id.");
        var scheme = State.SchemeInfos[input.SchemeId];
        if (Context.Sender != scheme.Manager && Context.Sender !=
            Context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName))
        {
            throw new AssertionException("Only manager or token holder contract can add beneficiary.");
        }

        var profitDetails = State.ProfitDetailsMap[input.SchemeId][input.BeneficiaryShare.Beneficiary];
        ProfitDetail fixingDetail = null;
        if (input.ProfitDetailId != null)
        {
            fixingDetail = profitDetails.Details.SingleOrDefault(d => d.Id == input.ProfitDetailId);
        }

        if (fixingDetail == null)
        {
            fixingDetail = profitDetails.Details.OrderBy(d => d.StartPeriod)
                .FirstOrDefault(d => d.Shares == input.BeneficiaryShare.Shares);
        }

        if (fixingDetail == null)
        {
            throw new AssertionException("Cannot find proper profit detail to fix.");
        }

        var newDetail = fixingDetail.Clone();
        newDetail.StartPeriod = input.StartPeriod == 0 ? fixingDetail.StartPeriod : input.StartPeriod;
        newDetail.EndPeriod = input.EndPeriod == 0 ? fixingDetail.EndPeriod : input.EndPeriod;
        profitDetails.Details.Remove(fixingDetail);
        profitDetails.Details.Add(newDetail);
        State.ProfitDetailsMap[input.SchemeId][input.BeneficiaryShare.Beneficiary] = profitDetails;
        return new Empty();
    }

    private RemovedDetails RemoveProfitDetails(Scheme scheme, Address beneficiary, Hash profitDetailId = null)
    {
        var removedDetails = new RemovedDetails();

        var profitDetails = State.ProfitDetailsMap[scheme.SchemeId][beneficiary];
        if (profitDetails == null)
        {
            return removedDetails;
        }

        var detailsCanBeRemoved = scheme.CanRemoveBeneficiaryDirectly
            ? profitDetails.Details.Where(d => !d.IsWeightRemoved).ToList()
            : profitDetails.Details
                .Where(d => d.EndPeriod < scheme.CurrentPeriod && !d.IsWeightRemoved).ToList();

        if (profitDetailId != null && profitDetails.Details.Any(d => d.Id == profitDetailId) && detailsCanBeRemoved.All(d => d.Id != profitDetailId))
        {
            detailsCanBeRemoved.Add(profitDetails.Details.Single(d => d.Id == profitDetailId));
        }

        if (detailsCanBeRemoved.Any())
        {
            foreach (var profitDetail in detailsCanBeRemoved)
            {
                profitDetail.IsWeightRemoved = true;
                if (profitDetail.LastProfitPeriod >= scheme.CurrentPeriod)
                {
                    profitDetails.Details.Remove(profitDetail);
                }
                else if (profitDetail.EndPeriod >= scheme.CurrentPeriod)
                {
                    profitDetail.EndPeriod = scheme.CurrentPeriod.Sub(1);
                }

                removedDetails.TryAdd(scheme.CurrentPeriod, profitDetail.Shares);
            }

            Context.LogDebug(() => $"ProfitDetails after removing expired details: {profitDetails}");
        }
        else
        {
            var weightCanBeRemoved = profitDetails.Details
                .Where(d => d.EndPeriod == scheme.CurrentPeriod && !d.IsWeightRemoved).ToList();
            foreach (var profitDetail in weightCanBeRemoved)
            {
                profitDetail.IsWeightRemoved = true;
            }

            var weights = weightCanBeRemoved.Sum(d => d.Shares);
            removedDetails.Add(0, weights);
        }

        // Clear old profit details.
        if (profitDetails.Details.Count != 0)
        {
            State.ProfitDetailsMap[scheme.SchemeId][beneficiary] = profitDetails;
        }
        else
        {
            State.ProfitDetailsMap[scheme.SchemeId].Remove(beneficiary);
        }

        return removedDetails;
    }

    public override Empty AddBeneficiaries(AddBeneficiariesInput input)
    {
        foreach (var beneficiaryShare in input.BeneficiaryShares)
            AddBeneficiary(new AddBeneficiaryInput
            {
                SchemeId = input.SchemeId,
                BeneficiaryShare = beneficiaryShare,
                EndPeriod = input.EndPeriod
            });

        return new Empty();
    }

    public override Empty RemoveBeneficiaries(RemoveBeneficiariesInput input)
    {
        foreach (var beneficiary in input.Beneficiaries)
            RemoveBeneficiary(new RemoveBeneficiaryInput
            {
                SchemeId = input.SchemeId, Beneficiary = beneficiary
            });

        return new Empty();
    }

    /// <summary>
    ///     Will burn/destroy a certain amount of profits if `input.Period` is less than 0.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Empty DistributeProfits(DistributeProfitsInput input)
    {
        if (input.AmountsMap.Any())
            Assert(input.AmountsMap.All(a => !string.IsNullOrEmpty(a.Key)), "Invalid token symbol.");

        var scheme = State.SchemeInfos[input.SchemeId];
        Assert(scheme != null, "Scheme not found.");

        // ReSharper disable once PossibleNullReferenceException
        Assert(Context.Sender == scheme.Manager || Context.Sender ==
            Context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName),
            "Only manager or token holder contract can distribute profits.");

        var amountMap = input.AmountsMap;
        var actualAmountMap = new Dictionary<string, long>();

        if (amountMap.Any())
        {
            // Follow the input.
            foreach (var pair in amountMap)
            {
                var symbol = pair.Key;
                var amount = pair.Value;
                AssertTokenExists(symbol);
                var actualAmount = pair.Value == 0
                    ? GetBalance(scheme.VirtualAddress, symbol)
                    : amount;
                actualAmountMap.Add(symbol, actualAmount);
            }
        }
        else
        {
            if (scheme.IsReleaseAllBalanceEveryTimeByDefault && scheme.ReceivedTokenSymbols.Any())
            {
                // Prepare to distribute all from general ledger.
                foreach (var symbol in scheme.ReceivedTokenSymbols)
                {
                    var balance = GetBalance(scheme.VirtualAddress, symbol);
                    actualAmountMap.Add(symbol, balance);
                }
            }
        }

        var totalShares = scheme.TotalShares;
        var isDistributable = false;

        if (scheme.DelayDistributePeriodCount > 0)
        {
            if (scheme.CachedDelayTotalShares.ContainsKey(input.Period))
            {
                totalShares = scheme.CachedDelayTotalShares[input.Period];
                scheme.CachedDelayTotalShares.Remove(input.Period);
                isDistributable = true;
            }

            var delayToPeriod = input.Period.Add(scheme.DelayDistributePeriodCount);
            scheme.CachedDelayTotalShares[delayToPeriod] = scheme.TotalShares;
        }
        else
        {
            isDistributable = true;
        }

        if (input.Period != scheme.CurrentPeriod)
        {
            throw new AssertionException(
                $"Invalid period. When release scheme {input.SchemeId.ToHex()} of period {input.Period}. Current period is {scheme.CurrentPeriod}");
        }

        if (input.Period < 0 || !isDistributable)
        {
            BurnProfits(scheme, scheme.CurrentPeriod, actualAmountMap);
            return new Empty();
        }

        var periodVirtualAddress =
            GetDistributedPeriodProfitsVirtualAddress(scheme.SchemeId, scheme.CurrentPeriod);

        Context.LogDebug(() => $"Receiving virtual address: {periodVirtualAddress}");

        MarkAsDistributed(scheme.SchemeId, scheme.CurrentPeriod, totalShares, actualAmountMap);

        PerformDistributeProfits(actualAmountMap, scheme, totalShares, periodVirtualAddress);

        MoveToNextPeriod(scheme.SchemeId);

        return new Empty();
    }

    private void AssertTokenExists(string symbol)
    {
        if (string.IsNullOrEmpty(State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput { Symbol = symbol })
                .TokenName))
        {
            throw new AssertionException($"Token {symbol} not exists.");
        }
    }

    private long GetBalance(Address address, string symbol)
    {
        return State.TokenContract.GetBalance.Call(new GetBalanceInput
        {
            Owner = address,
            Symbol = symbol
        }).Balance;
    }

    private void MarkAsDistributed(Hash schemeId, long period, long totalShares,
        Dictionary<string, long> actualAmountMap)
    {
        var periodVirtualAddress = GetDistributedPeriodProfitsVirtualAddress(schemeId, period);
        var distributedProfitsInfo =
            GetDistributedProfitsInfo(new SchemePeriod { SchemeId = schemeId, Period = period });
        distributedProfitsInfo.TotalShares = totalShares;
        distributedProfitsInfo.IsReleased = true;
        foreach (var profits in actualAmountMap)
        {
            var symbol = profits.Key;
            var amount = profits.Value;
            if (distributedProfitsInfo.AmountsMap.ContainsKey(symbol))
            {
                distributedProfitsInfo.AmountsMap[symbol] = distributedProfitsInfo.AmountsMap[symbol].Add(amount);
            }
            else
            {
                distributedProfitsInfo.AmountsMap[symbol] = amount;
            }
        }

        State.DistributedProfitsMap[periodVirtualAddress] = distributedProfitsInfo;

        Context.Fire(new DistributedPeriodInfoChanged
        {
            SchemeId = schemeId,
            Period = period,
            VirtualAddress = periodVirtualAddress,
            ProfitsMapList = new ReceivedProfitsMapList
            {
                Value =
                {
                    new ReceivedProfitsMap
                    {
                        Value = { distributedProfitsInfo.AmountsMap }
                    }
                }
            },
            IsReleased = true,
            TotalShares = totalShares
        });
    }

    private void MoveToNextPeriod(Hash schemeId)
    {
        State.SchemeInfos[schemeId].CurrentPeriod = State.SchemeInfos[schemeId].CurrentPeriod.Add(1);
    }

    /// <summary>
    ///     Just burn balance in general ledger.
    /// </summary>
    /// <param name="scheme"></param>
    /// <param name="period"></param>
    /// <param name="amountMap"></param>
    /// <returns></returns>
    private void BurnProfits(Scheme scheme, long period, Dictionary<string, long> amountMap)
    {
        MoveToNextPeriod(scheme.SchemeId);
        var actualAmountMap = new Dictionary<string, long>();
        foreach (var profits in amountMap)
        {
            var symbol = profits.Key;
            var amount = profits.Value;
            if (amount > 0)
            {
                var balanceOfToken = State.TokenContract.GetBalance.Call(new GetBalanceInput
                {
                    Owner = scheme.VirtualAddress,
                    Symbol = symbol
                });
                if (balanceOfToken.Balance < amount)
                    continue;
                Context.SendVirtualInline(scheme.SchemeId, State.TokenContract.Value,
                    nameof(State.TokenContract.Transfer), new TransferInput
                    {
                        To = Context.Self,
                        Amount = amount,
                        Symbol = symbol
                    }.ToByteString());
                State.TokenContract.Burn.Send(new BurnInput
                {
                    Amount = amount,
                    Symbol = symbol
                });
                actualAmountMap.Add(symbol, -amount);
            }
        }

        MarkAsDistributed(scheme.SchemeId, period, 0, actualAmountMap);
    }

    private void PerformDistributeProfits(Dictionary<string, long> profitsMap, Scheme scheme, long totalShares,
        Address profitsReceivingVirtualAddress)
    {
        foreach (var profits in profitsMap)
        {
            var symbol = profits.Key;
            var amount = profits.Value;
            var remainAmount = DistributeProfitsForSubSchemes(symbol, amount, scheme, totalShares);
            Context.LogDebug(() => $"Distributing {remainAmount} {symbol} tokens.");
            // Transfer remain amount to individuals' receiving profits address.
            if (remainAmount != 0)
            {
                Context.SendVirtualInline(scheme.SchemeId, State.TokenContract.Value,
                    nameof(State.TokenContract.Transfer), new TransferInput
                    {
                        To = profitsReceivingVirtualAddress,
                        Amount = remainAmount,
                        Symbol = symbol
                    }.ToByteString());
            }
        }
    }

    private long DistributeProfitsForSubSchemes(string symbol, long totalAmount, Scheme scheme, long totalShares)
    {
        Context.LogDebug(() => $"Sub schemes count: {scheme.SubSchemes.Count}");
        var remainAmount = totalAmount;
        foreach (var subSchemeShares in scheme.SubSchemes)
        {
            Context.LogDebug(() => $"Releasing {subSchemeShares.SchemeId}");

            // General ledger of this sub profit scheme.
            var subItemVirtualAddress = Context.ConvertVirtualAddressToContractAddress(subSchemeShares.SchemeId);

            if (State.TokenContract.Value == null)
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

            var distributeAmount = SafeCalculateProfits(subSchemeShares.Shares, totalAmount, totalShares);
            if (distributeAmount != 0)
            {
                Context.SendVirtualInline(scheme.SchemeId, State.TokenContract.Value,
                    nameof(State.TokenContract.Transfer), new TransferInput
                    {
                        To = subItemVirtualAddress,
                        Amount = distributeAmount,
                        Symbol = symbol
                    }.ToByteString());
            }

            remainAmount = remainAmount.Sub(distributeAmount);

            // Update current_period of detail of sub profit scheme.
            var subItemDetail = State.ProfitDetailsMap[scheme.SchemeId][subItemVirtualAddress];
            foreach (var detail in subItemDetail.Details) detail.LastProfitPeriod = scheme.CurrentPeriod;

            State.ProfitDetailsMap[scheme.SchemeId][subItemVirtualAddress] = subItemDetail;

            // Update sub scheme.
            var subScheme = State.SchemeInfos[subSchemeShares.SchemeId];
            if (!subScheme.ReceivedTokenSymbols.Contains(symbol))
            {
                subScheme.ReceivedTokenSymbols.Add(symbol);
                State.SchemeInfos[subSchemeShares.SchemeId] = subScheme;
            }
        }

        return remainAmount;
    }

    public override Empty ContributeProfits(ContributeProfitsInput input)
    {
        AssertTokenExists(input.Symbol);
        if (input.Amount <= 0)
        {
            throw new AssertionException("Amount need to greater than 0.");
        }

        var scheme = State.SchemeInfos[input.SchemeId];
        if (scheme == null)
        {
            throw new AssertionException("Scheme not found.");
        }

        Address toAddress;
        if (input.Period == 0)
        {
            toAddress = scheme.VirtualAddress;
        }
        else
        {
            if (input.Period < scheme.CurrentPeriod)
            {
                throw new AssertionException("Invalid contributing period.");
            }

            var periodVirtualAddress = GetDistributedPeriodProfitsVirtualAddress(scheme.SchemeId, input.Period);
            AddProfits(scheme.SchemeId, input.Period, input.Symbol, input.Amount);
            toAddress = periodVirtualAddress;
        }

        State.TokenContract.TransferFrom.Send(new TransferFromInput
        {
            From = Context.Sender,
            To = toAddress,
            Symbol = input.Symbol,
            Amount = input.Amount,
            Memo = $"Add {input.Amount} dividends."
        });

        // If someone directly use virtual address to do the contribution, won't sense the token symbol he was using.
        if (!State.SchemeInfos[scheme.SchemeId].ReceivedTokenSymbols.Contains(input.Symbol))
        {
            State.SchemeInfos[scheme.SchemeId].ReceivedTokenSymbols.Add(input.Symbol);
        }

        return new Empty();
    }
    
    public void AddProfits(Hash schemeId, long period, string symbol, long amount)
    {
        var periodVirtualAddress = GetDistributedPeriodProfitsVirtualAddress(schemeId, period);
        var distributedProfitsInfo = State.DistributedProfitsMap[periodVirtualAddress];
        if (distributedProfitsInfo == null)
        {
            distributedProfitsInfo = new DistributedProfitsInfo
            {
                AmountsMap = { { symbol, amount } }
            };
        }
        else
        {
            if (distributedProfitsInfo.IsReleased)
            {
                throw new AssertionException($"Scheme of period {period} already released.");
            }

            distributedProfitsInfo.AmountsMap[symbol] =
                distributedProfitsInfo.AmountsMap[symbol].Add(amount);
        }

        State.DistributedProfitsMap[periodVirtualAddress] = distributedProfitsInfo;

        Context.Fire(new DistributedPeriodInfoChanged
        {
            SchemeId = schemeId,
            Period = period,
            VirtualAddress = periodVirtualAddress,
            ProfitsMapList = new ReceivedProfitsMapList
            {
                Value =
                {
                    new ReceivedProfitsMap
                    {
                        Value = { distributedProfitsInfo.AmountsMap }
                    }
                }
            }
        });
    }

    public override Empty ResetManager(ResetManagerInput input)
    {
        var scheme = State.SchemeInfos[input.SchemeId];
        Assert(scheme != null, "Scheme not found.");

        // ReSharper disable once PossibleNullReferenceException
        Assert(Context.Sender == scheme.Manager, "Only scheme manager can reset manager.");
        Assert(input.NewManager.Value.Any(), "Invalid new sponsor.");

        // Transfer managing scheme id.
        var oldManagerSchemeIds = State.ManagingSchemeIds[scheme.Manager];
        oldManagerSchemeIds.SchemeIds.Remove(input.SchemeId);
        State.ManagingSchemeIds[scheme.Manager] = oldManagerSchemeIds;
        var newManagerSchemeIds = State.ManagingSchemeIds[input.NewManager] ?? new CreatedSchemeIds();
        newManagerSchemeIds.SchemeIds.Add(input.SchemeId);
        State.ManagingSchemeIds[input.NewManager] = newManagerSchemeIds;

        scheme.Manager = input.NewManager;
        State.SchemeInfos[input.SchemeId] = scheme;
        return new Empty();
    }

    /// <summary>
    ///     Gain the profit form SchemeId from Details.lastPeriod to scheme.currentPeriod - 1;
    /// </summary>
    /// <param name="input">ClaimProfitsInput</param>
    /// <returns></returns>
    public override Empty ClaimProfits(ClaimProfitsInput input)
    {
        var scheme = State.SchemeInfos[input.SchemeId];
        if (scheme == null) throw new AssertionException("Scheme not found.");
        var beneficiary = input.Beneficiary ?? Context.Sender;
        var profitDetails = State.ProfitDetailsMap[input.SchemeId][beneficiary];
        if (profitDetails == null) throw new AssertionException("Profit details not found.");

        Context.LogDebug(
            () => $"{Context.Sender} is trying to profit from {input.SchemeId.ToHex()} for {beneficiary}.");

        var availableDetails = profitDetails.Details.Where(d =>
            d.LastProfitPeriod == 0 ? d.EndPeriod >= d.StartPeriod : d.EndPeriod >= d.LastProfitPeriod).ToList();
        var profitableDetails = availableDetails.Where(d => d.LastProfitPeriod < scheme.CurrentPeriod).ToList();

        Context.LogDebug(() =>
            $"Profitable details: {profitableDetails.Aggregate("\n", (profit1, profit2) => profit1.ToString() + "\n" + profit2)}");

        // Only can get profit from last profit period to actual last period (profit.CurrentPeriod - 1),
        // because current period not released yet.
        for (var i = 0;
             i < Math.Min(ProfitContractConstants.ProfitReceivingLimitForEachTime, profitableDetails.Count);
             i++)
        {
            var profitDetail = profitableDetails[i];
            if (profitDetail.LastProfitPeriod == 0)
                // This detail never performed profit before.
                profitDetail.LastProfitPeriod = profitDetail.StartPeriod;

            ProfitAllPeriods(scheme, profitDetail, beneficiary);
        }

        var profitDetailsToRemove = profitableDetails
            .Where(profitDetail =>
                profitDetail.LastProfitPeriod > profitDetail.EndPeriod && !profitDetail.IsWeightRemoved).ToList();
        var sharesToRemove =
            profitDetailsToRemove.Aggregate(0L, (current, profitDetail) => current.Add(profitDetail.Shares));
        scheme.TotalShares = scheme.TotalShares.Sub(sharesToRemove);
        foreach (var delayToPeriod in scheme.CachedDelayTotalShares.Keys)
        {
            scheme.CachedDelayTotalShares[delayToPeriod] =
                scheme.CachedDelayTotalShares[delayToPeriod].Sub(sharesToRemove);
        }

        State.SchemeInfos[scheme.SchemeId] = scheme;

        foreach (var profitDetail in profitDetailsToRemove)
        {
            availableDetails.Remove(profitDetail);
        }

        State.ProfitDetailsMap[input.SchemeId][beneficiary] = new ProfitDetails { Details = { availableDetails } };

        return new Empty();
    }

    private Dictionary<string, long> ProfitAllPeriods(Scheme scheme, ProfitDetail profitDetail, Address beneficiary,
        bool isView = false, string targetSymbol = null)
    {
        var profitsMap = new Dictionary<string, long>();
        var lastProfitPeriod = profitDetail.LastProfitPeriod;

        var symbols = targetSymbol == null ? scheme.ReceivedTokenSymbols.ToList() : new List<string> { targetSymbol };

        foreach (var symbol in symbols)
        {
            var totalAmount = 0L;
            for (var period = profitDetail.LastProfitPeriod;
                 period <= (profitDetail.EndPeriod == long.MaxValue
                     ? Math.Min(scheme.CurrentPeriod - 1,
                         profitDetail.LastProfitPeriod.Add(ProfitContractConstants
                             .MaximumProfitReceivingPeriodCountOfOneTime))
                     : Math.Min(scheme.CurrentPeriod - 1, profitDetail.EndPeriod));
                 period++)
            {
                var periodToPrint = period;
                var detailToPrint = profitDetail;
                var distributedPeriodProfitsVirtualAddress =
                    GetDistributedPeriodProfitsVirtualAddress(scheme.SchemeId, period);
                var distributedProfitsInformation =
                    State.DistributedProfitsMap[distributedPeriodProfitsVirtualAddress];
                if (distributedProfitsInformation == null || distributedProfitsInformation.TotalShares == 0 ||
                    !distributedProfitsInformation.AmountsMap.Any() ||
                    !distributedProfitsInformation.AmountsMap.ContainsKey(symbol))
                    continue;

                var amount = SafeCalculateProfits(profitDetail.Shares,
                    distributedProfitsInformation.AmountsMap[symbol], distributedProfitsInformation.TotalShares);

                if (!isView)
                {
                    Context.LogDebug(() =>
                        $"{beneficiary} is profiting {amount} {symbol} tokens from {scheme.SchemeId.ToHex()} in period {periodToPrint}." +
                        $"Sender's Shares: {detailToPrint.Shares}, total Shares: {distributedProfitsInformation.TotalShares}");
                    if (distributedProfitsInformation.IsReleased && amount > 0)
                    {
                        if (State.TokenContract.Value == null)
                            State.TokenContract.Value =
                                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

                        Context.SendVirtualInline(
                            GeneratePeriodVirtualAddressFromHash(scheme.SchemeId, period),
                            State.TokenContract.Value,
                            nameof(State.TokenContract.Transfer), new TransferInput
                            {
                                To = beneficiary,
                                Symbol = symbol,
                                Amount = amount
                            }.ToByteString());

                        Context.Fire(new ProfitsClaimed
                        {
                            Beneficiary = beneficiary,
                            Symbol = symbol,
                            Amount = amount,
                            ClaimerShares = detailToPrint.Shares,
                            TotalShares = distributedProfitsInformation.TotalShares,
                            Period = periodToPrint
                        });
                    }

                    lastProfitPeriod = period + 1;
                }

                totalAmount = totalAmount.Add(amount);
            }

            profitsMap.Add(symbol, totalAmount);
        }

        profitDetail.LastProfitPeriod = lastProfitPeriod;

        return profitsMap;
    }

    private void ValidateContractState(ContractReferenceState state, string contractSystemName)
    {
        if (state.Value != null)
            return;
        state.Value = Context.GetContractAddressByName(contractSystemName);
    }

    private Scheme GetNewScheme(CreateSchemeInput input, Hash schemeId, Address manager)
    {
        var scheme = new Scheme
        {
            SchemeId = schemeId,
            // The address of general ledger for current profit scheme.
            VirtualAddress = Context.ConvertVirtualAddressToContractAddress(schemeId),
            Manager = manager,
            ProfitReceivingDuePeriodCount = input.ProfitReceivingDuePeriodCount,
            CurrentPeriod = 1,
            IsReleaseAllBalanceEveryTimeByDefault = input.IsReleaseAllBalanceEveryTimeByDefault,
            DelayDistributePeriodCount = input.DelayDistributePeriodCount,
            CanRemoveBeneficiaryDirectly = input.CanRemoveBeneficiaryDirectly
        };

        return scheme;
    }

    private static long SafeCalculateProfits(long totalAmount, long shares, long totalShares)
    {
        var decimalTotalAmount = (decimal)totalAmount;
        var decimalShares = (decimal)shares;
        var decimalTotalShares = (decimal)totalShares;
        return (long)(decimalTotalAmount * decimalShares / decimalTotalShares);
    }

    private Hash GenerateSchemeId(CreateSchemeInput createSchemeInput)
    {
        var manager = createSchemeInput.Manager ?? Context.Sender;
        if (createSchemeInput.Token != null)
            return Context.GenerateId(Context.Self, createSchemeInput.Token);
        var createdSchemeCount = State.ManagingSchemeIds[manager]?.SchemeIds.Count ?? 0;
        return Context.GenerateId(Context.Self, createdSchemeCount.ToBytes(false));
    }
}