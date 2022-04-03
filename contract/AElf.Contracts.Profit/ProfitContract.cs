using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Profit
{
    /// <summary>
    /// Let's imagine a scenario:
    /// 1. Ean creates a profit scheme FOO: Ean calls CreateScheme. We call this profit scheme PI_FOO.
    /// 2. GL creates another profit scheme BAR: GL calls CreateScheme. We call this profit scheme PI_BAR.
    /// 3. Ean (as the creator of PI_FOO) register PI_BAR as a sub profit scheme as PI_FOO:
    /// Ean call RemoveSubScheme (SchemeId: PI_BAR's profit id, Shares : 1)
    /// 4. Anil has an account which address is ADDR_Anil.
    /// 5. Ean registers address ADDR_Anil as a profit Beneficiary of PI_FOO: Ean calls AddBeneficiary (Beneficiary: ADDR_Anil, Shares : 1)
    /// 6: Now PI_FOO is organized like this:
    ///         PI_FOO
    ///        /      \
    ///       1        1
    ///      /          \
    ///    PI_BAR     ADDR_Anil
    ///    (Total Shares is 2)
    /// 7. Ean adds some ELF tokens to PI_FOO: Ean calls DistributeProfits (Symbol: "ELF", Amount: 1000L, Period: 1)
    /// 8. Ean calls DistributeProfits: Balance of PI_BAR is 500L (PI_BAR's general ledger balance, also we can say balance of virtual address of PI_BAR is 500L),
    /// 9. Balance of PI_FOO's virtual address of first period is 500L.
    /// 10. Anil can only get his profits by calling Profit (SchemeId: PI_BAR's profit id, Symbol: "ELF")
    /// </summary>
    public partial class ProfitContract : ProfitContractImplContainer.ProfitContractImplBase
    {
        /// <summary>
        /// Create a Scheme of profit distribution.
        /// At the first time, the scheme's id is unknown,it may create by transaction id and createdSchemeIds;
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Hash CreateScheme(CreateSchemeInput input)
        {
            MakeSureReferenceStateAddressSet(State.TokenContract, SmartContractConstants.TokenContractSystemName);
            var schemeId = GenerateSchemeId(input);
            GetProfitSchemeManager().CreateNewScheme(new Scheme
            {
                SchemeId = schemeId,
                // The address of general ledger for current profit scheme.
                VirtualAddress = Context.ConvertVirtualAddressToContractAddress(schemeId),
                Manager = input.Manager ?? Context.Sender,
                ProfitReceivingDuePeriodCount = input.ProfitReceivingDuePeriodCount,
                CurrentPeriod = 1,
                IsReleaseAllBalanceEveryTimeByDefault = input.IsReleaseAllBalanceEveryTimeByDefault,
                DelayDistributePeriodCount = input.DelayDistributePeriodCount,
                CanRemoveBeneficiaryDirectly = input.CanRemoveBeneficiaryDirectly
            });

            return schemeId;
        }

        /// <summary>
        /// Add a child to a existed scheme.
        /// </summary>
        /// <param name="input">AddSubSchemeInput</param>
        /// <returns></returns>
        public override Empty AddSubScheme(AddSubSchemeInput input)
        {
            var profitSchemeManager = GetProfitSchemeManager();
            profitSchemeManager.AddSubScheme(input.SchemeId, input.SubSchemeId, input.SubSchemeShares);
            var beneficiaryManager = GetBeneficiaryManager(profitSchemeManager);
            beneficiaryManager.AddBeneficiary(input.SchemeId, new BeneficiaryShare
            {
                Beneficiary = Context.ConvertVirtualAddressToContractAddress(input.SubSchemeId),
                Shares = input.SubSchemeShares
            }, long.MaxValue); // Profits may last forever in `AddSubScheme` case.
            return new Empty();
        }

        public override Empty RemoveSubScheme(RemoveSubSchemeInput input)
        {
            var profitSchemeManager = GetProfitSchemeManager();
            profitSchemeManager.RemoveSubScheme(input.SchemeId, input.SubSchemeId);
            var beneficiaryManager = GetBeneficiaryManager(profitSchemeManager);
            beneficiaryManager.RemoveBeneficiary(input.SchemeId,
                Context.ConvertVirtualAddressToContractAddress(input.SubSchemeId), true);
            return new Empty();
        }

        public override Empty AddBeneficiary(AddBeneficiaryInput input)
        {
            GetBeneficiaryManager().AddBeneficiary(input.SchemeId, input.BeneficiaryShare, input.EndPeriod);
            return new Empty();
        }

        public override Empty RemoveBeneficiary(RemoveBeneficiaryInput input)
        {
            GetBeneficiaryManager().RemoveBeneficiary(input.SchemeId, input.Beneficiary);
            return new Empty();
        }

        public override Empty AddBeneficiaries(AddBeneficiariesInput input)
        {
            var beneficiaryManager = GetBeneficiaryManager();
            foreach (var beneficiaryShare in input.BeneficiaryShares)
            {
                beneficiaryManager.AddBeneficiary(input.SchemeId, beneficiaryShare, input.EndPeriod);
            }

            return new Empty();
        }

        public override Empty RemoveBeneficiaries(RemoveBeneficiariesInput input)
        {
            var beneficiaryManager = GetBeneficiaryManager();
            foreach (var beneficiary in input.Beneficiaries)
            {
                beneficiaryManager.RemoveBeneficiary(input.SchemeId, beneficiary);
            }

            return new Empty();
        }

        /// <summary>
        /// Will burn/destroy a certain amount of profits if `input.Period` is less than 0.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty DistributeProfits(DistributeProfitsInput input)
        {
            if (input.AmountsMap.Any())
            {
                Assert(input.AmountsMap.All(a => !string.IsNullOrEmpty(a.Key)), "Invalid token symbol.");
            }

            var scheme = State.SchemeInfos[input.SchemeId];
            Assert(scheme != null, "Scheme not found.");

            // ReSharper disable once PossibleNullReferenceException
            Assert(Context.Sender == scheme.Manager || Context.Sender ==
                   Context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName),
                "Only manager can distribute profits.");

            MakeSureReferenceStateAddressSet(State.TokenContract, SmartContractConstants.TokenContractSystemName);

            var profitsMap = new Dictionary<string, long>();
            if (input.AmountsMap.Any())
            {
                foreach (var amount in input.AmountsMap)
                {
                    var actualAmount = amount.Value == 0
                        ? State.TokenContract.GetBalance.Call(new GetBalanceInput
                        {
                            Owner = scheme.VirtualAddress,
                            Symbol = amount.Key
                        }).Balance
                        : amount.Value;
                    profitsMap.Add(amount.Key, actualAmount);
                }
            }
            else
            {
                if (scheme.IsReleaseAllBalanceEveryTimeByDefault && scheme.ReceivedTokenSymbols.Any())
                {
                    // Prepare to distribute all from general ledger.
                    foreach (var symbol in scheme.ReceivedTokenSymbols)
                    {
                        var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
                        {
                            Owner = scheme.VirtualAddress,
                            Symbol = symbol
                        }).Balance;
                        profitsMap.Add(symbol, balance);
                    }
                }
            }

            var totalShares = scheme.TotalShares;

            if (scheme.DelayDistributePeriodCount > 0)
            {
                scheme.CachedDelayTotalShares.Add(input.Period.Add(scheme.DelayDistributePeriodCount), totalShares);
                if (scheme.CachedDelayTotalShares.ContainsKey(input.Period))
                {
                    totalShares = scheme.CachedDelayTotalShares[input.Period];
                    scheme.CachedDelayTotalShares.Remove(input.Period);
                }
                else
                {
                    totalShares = 0;
                }
            }

            var releasingPeriod = scheme.CurrentPeriod;
            Assert(input.Period == releasingPeriod,
                $"Invalid period. When release scheme {input.SchemeId.ToHex()} of period {input.Period}. Current period is {releasingPeriod}");

            var profitsReceivingVirtualAddress =
                GetDistributedPeriodProfitsVirtualAddress(scheme.SchemeId, releasingPeriod);

            if (input.Period < 0 || totalShares <= 0)
            {
                return BurnProfits(input.Period, profitsMap, scheme, profitsReceivingVirtualAddress);
            }

            Context.LogDebug(() => $"Receiving virtual address: {profitsReceivingVirtualAddress}");

            UpdateDistributedProfits(profitsMap, profitsReceivingVirtualAddress, totalShares);

            PerformDistributeProfits(profitsMap, scheme, totalShares, profitsReceivingVirtualAddress);

            scheme.CurrentPeriod = input.Period.Add(1);

            State.SchemeInfos[input.SchemeId] = scheme;

            return new Empty();
        }

        /// <summary>
        /// Just burn balance in general ledger.
        /// </summary>
        /// <param name="period"></param>
        /// <param name="amountMap"></param>
        /// <param name="scheme"></param>
        /// <param name="profitsReceivingVirtualAddress"></param>
        /// <returns></returns>
        private Empty BurnProfits(long period, Dictionary<string, long> amountMap, Scheme scheme,
            Address profitsReceivingVirtualAddress)
        {
            scheme.CurrentPeriod = period.Add(1);

            var distributedProfitsInfo = new DistributedProfitsInfo
            {
                IsReleased = true
            };
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
                    distributedProfitsInfo.AmountsMap.Add(symbol, -amount);
                }
            }

            State.SchemeInfos[scheme.SchemeId] = scheme;
            State.DistributedProfitsMap[profitsReceivingVirtualAddress] = distributedProfitsInfo;
            return new Empty();
        }

        private void UpdateDistributedProfits(Dictionary<string, long> profitsMap,
            Address profitsReceivingVirtualAddress, long totalShares)
        {
            var distributedProfitsInformation =
                State.DistributedProfitsMap[profitsReceivingVirtualAddress] ??
                new DistributedProfitsInfo();

            distributedProfitsInformation.TotalShares = totalShares;
            distributedProfitsInformation.IsReleased = true;

            foreach (var profits in profitsMap)
            {
                var symbol = profits.Key;
                var amount = profits.Value;
                var balanceOfVirtualAddressForCurrentPeriod = State.TokenContract.GetBalance.Call(new GetBalanceInput
                {
                    Owner = profitsReceivingVirtualAddress,
                    Symbol = symbol
                }).Balance;
                distributedProfitsInformation.AmountsMap[symbol] = amount.Add(balanceOfVirtualAddressForCurrentPeriod);
            }

            State.DistributedProfitsMap[profitsReceivingVirtualAddress] = distributedProfitsInformation;
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
                {
                    State.TokenContract.Value =
                        Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
                }

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
                foreach (var detail in subItemDetail.Details)
                {
                    detail.LastProfitPeriod = scheme.CurrentPeriod;
                }

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
            MakeSureReferenceStateAddressSet(State.TokenContract, SmartContractConstants.TokenContractSystemName);
            GetProfitService().Contribute(input.SchemeId, input.Period, input.Symbol, input.Amount);
            return new Empty();
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
        /// Gain the profit form SchemeId from Details.lastPeriod to scheme.currentPeriod - 1;
        /// </summary>
        /// <param name="input">ClaimProfitsInput</param>
        /// <returns></returns>
        public override Empty ClaimProfits(ClaimProfitsInput input)
        {
            var scheme = State.SchemeInfos[input.SchemeId];
            if (scheme == null)
            {
                throw new AssertionException("Scheme not found.");
            }
            var beneficiary = input.Beneficiary ?? Context.Sender;
            var profitDetails = State.ProfitDetailsMap[input.SchemeId][beneficiary];
            if (profitDetails == null)
            {
                throw new AssertionException("Profit details not found.");
            }

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
                {
                    // This detail never performed profit before.
                    profitDetail.LastProfitPeriod = profitDetail.StartPeriod;
                }

                ProfitAllPeriods(scheme, profitDetail, beneficiary);
            }

            State.ProfitDetailsMap[input.SchemeId][beneficiary] = new ProfitDetails {Details = {availableDetails}};

            return new Empty();
        }

        private Dictionary<string, long> ProfitAllPeriods(Scheme scheme, ProfitDetail profitDetail, Address beneficiary,
            bool isView = false, string targetSymbol = null)
        {
            var profitsMap = new Dictionary<string, long>();
            var lastProfitPeriod = profitDetail.LastProfitPeriod;

            var symbols = targetSymbol == null ? scheme.ReceivedTokenSymbols.ToList() : new List<string> {targetSymbol};

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
                    {
                        continue;
                    }

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
                            {
                                State.TokenContract.Value =
                                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
                            }

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
    }
}