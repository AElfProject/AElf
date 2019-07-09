using System;
using System.Linq;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Profit
{
    /// <summary>
    /// Let's imagine a scenario:
    /// 1. Ean creates a profit item FOO: Ean calls CreateScheme. We call this profit item PI_FOO.
    /// 2. GL creates another profit item BAR: GL calls CreateScheme. We call this profit item PI_BAR.
    /// 3. Ean (as the creator of PI_FOO) register PI_BAR as a sub profit item as PI_FOO:
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
    public partial class ProfitContract : ProfitContractContainer.ProfitContractBase
    {
        /// <summary>
        /// Create a ProfitItem
        /// At the first time,the profitItem's id is unknown,it may create by transaction id and createdSchemeIds;
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Hash CreateScheme(CreateSchemeInput input)
        {
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            if (input.ProfitReceivingDuePeriodCount == 0)
            {
                input.ProfitReceivingDuePeriodCount = ProfitContractConsts.DefaultProfitReceivingDuePeriodCount;
            }

            var schemeId = Context.TransactionId;
            // Why? Because one transaction may create many profit items via inline transactions.
            var createdSchemeIds = State.CreatedSchemeIds[Context.Sender]?.SchemeIds;
            if (createdSchemeIds != null && createdSchemeIds.Contains(schemeId))
            {
                // So we choose this way to avoid profit id conflicts in aforementioned situation.
                schemeId = Hash.FromTwoHashes(schemeId, createdSchemeIds.Last());
            }

            var scheme = new Scheme
            {
                SchemeId = schemeId,
                // The address of general ledger for current profit item.
                VirtualAddress = Context.ConvertVirtualAddressToContractAddress(schemeId),
                Creator = Context.Sender,
                ProfitReceivingDuePeriodCount = input.ProfitReceivingDuePeriodCount,
                CurrentPeriod = 1,
                IsReleaseAllBalanceEverytimeByDefault = input.IsReleaseAllBalanceEveryTimeByDefault
            };
            State.SchemeInfos[schemeId] = scheme;

            var schemeIds = State.CreatedSchemeIds[Context.Sender];
            if (schemeIds == null)
            {
                schemeIds = new CreatedSchemeIds
                {
                    SchemeIds = {schemeId}
                };
            }
            else
            {
                schemeIds.SchemeIds.Add(schemeId);
            }

            State.CreatedSchemeIds[Context.Sender] = schemeIds;

            Context.LogDebug(() => $"Created profit item {State.SchemeInfos[schemeId]}");

            Context.Fire(new SchemeCreated
            {
                SchemeId = scheme.SchemeId,
                Creator = scheme.Creator,
                IsReleaseAllBalanceEverytimeByDefault = scheme.IsReleaseAllBalanceEverytimeByDefault,
                ProfitReceivingDuePeriodCount = scheme.ProfitReceivingDuePeriodCount,
                VirtualAddress = scheme.VirtualAddress
            });
            return schemeId;
        }

        /// <summary>
        /// Register a SubProfitItem,binding to a ProfitItem as child.
        /// Then add the father ProfitItem's Shares.
        /// </summary>
        /// <param name="input">RemoveSubSchemeInput</param>
        /// <returns></returns>
        public override Empty AddSubScheme(AddSubSchemeInput input)
        {
            Assert(input.SchemeId != input.SubSchemeId, "Two profit items cannot be same.");
            Assert(input.SubSchemeShares > 0, "Shares of sub profit item should greater than 0.");

            var scheme = State.SchemeInfos[input.SchemeId];
            Assert(scheme != null, "Profit item not found.");

            if (scheme == null)
            {
                return new Empty();
            }

            Assert(Context.Sender == scheme.Creator, "Only creator can do the registration.");

            var subProfitItemId = input.SubSchemeId;
            var subProfitItem = State.SchemeInfos[subProfitItemId];
            Assert(subProfitItem != null, "Sub profit item not found.");

            if (subProfitItem == null)
            {
                return new Empty();
            }

            var subItemVirtualAddress = Context.ConvertVirtualAddressToContractAddress(subProfitItemId);
            AddBeneficiary(new AddBeneficiaryInput
            {
                SchemeId = input.SchemeId,
                BeneficiaryShare = new BeneficiaryShare
                {
                    Beneficiary = subItemVirtualAddress,
                    Shares = input.SubSchemeShares
                },
                EndPeriod = long.MaxValue
            });

            // Add a sub profit item.
            scheme.SubSchemes.Add(new SchemeBeneficiaryShare
            {
                SchemeId = input.SubSchemeId,
                Shares = input.SubSchemeShares
            });
            scheme.TotalShares = scheme.TotalShares.Add(input.SubSchemeShares);
            State.SchemeInfos[input.SchemeId] = scheme;

            return new Empty();
        }

        public override Empty RemoveSubScheme(RemoveSubSchemeInput input)
        {
            Assert(input.SchemeId != input.SubSchemeId, "Two profit items cannot be same.");

            var profitItem = State.SchemeInfos[input.SchemeId];
            Assert(profitItem != null, "Profit item not found.");

            if (profitItem == null)
            {
                return new Empty();
            }

            Assert(input.SubItemCreator == profitItem.Creator, "Only creator can do the Cancel.");

            var subProfitItemId = input.SubSchemeId;
            var subProfitItem = State.SchemeInfos[subProfitItemId];
            Assert(subProfitItem != null, "Sub profit item not found.");

            if (subProfitItem == null)
            {
                return new Empty();
            }

            var subItemVirtualAddress = Context.ConvertVirtualAddressToContractAddress(subProfitItemId);
            RemoveBeneficiary(new RemoveBeneficiaryInput
            {
                SchemeId = input.SchemeId,
                Beneficiary = subItemVirtualAddress
            });


            profitItem.SubSchemes.Remove(profitItem.SubSchemes.Single(d => d.SchemeId == input.SubSchemeId));
            State.SchemeInfos[input.SchemeId] = profitItem;

            return new Empty();
        }

        public override Empty AddBeneficiary(AddBeneficiaryInput input)
        {
            Assert(input.SchemeId != null, "Invalid profit id.");
            Assert(input.BeneficiaryShare?.Beneficiary != null, "Invalid beneficiary address.");
            Assert(input.BeneficiaryShare?.Shares >= 0, "Invalid share.");

            if (input.EndPeriod == 0)
            {
                // Which means this profit Beneficiary will never expired.
                input.EndPeriod = long.MaxValue;
            }

            var schemeId = input.SchemeId;
            var profitItem = State.SchemeInfos[schemeId];

            Assert(profitItem != null, "Profit item not found.");

            if (profitItem == null)
            {
                return new Empty();
            }

            Context.LogDebug(() =>
                $"{input.SchemeId}.\n End Period: {input.EndPeriod}, Current Period: {profitItem.CurrentPeriod}");

            Assert(input.EndPeriod >= profitItem.CurrentPeriod, "Invalid end period.");

            profitItem.TotalShares = profitItem.TotalShares.Add(input.BeneficiaryShare.Shares);

            State.SchemeInfos[schemeId] = profitItem;

            var profitDetail = new ProfitDetail
            {
                StartPeriod = profitItem.CurrentPeriod,
                EndPeriod = input.EndPeriod,
                Shares = input.BeneficiaryShare.Shares,
            };

            var currentProfitDetails = State.ProfitDetailsMap[schemeId][input.BeneficiaryShare.Beneficiary];
            if (currentProfitDetails == null)
            {
                currentProfitDetails = new ProfitDetails
                {
                    Details = {profitDetail}
                };
            }
            else
            {
                currentProfitDetails.Details.Add(profitDetail);
            }

            // Remove details too old.
            foreach (var detail in currentProfitDetails.Details.Where(
                d => d.EndPeriod != long.MaxValue && d.LastProfitPeriod >= d.EndPeriod &&
                     d.EndPeriod.Add(profitItem.ProfitReceivingDuePeriodCount) < profitItem.CurrentPeriod))
            {
                currentProfitDetails.Details.Remove(detail);
            }

            State.ProfitDetailsMap[schemeId][input.BeneficiaryShare.Beneficiary] = currentProfitDetails;

            Context.LogDebug(() =>
                $"Added {input.BeneficiaryShare.Shares} weights to profit item {input.SchemeId.ToHex()}: {profitDetail}");

            return new Empty();
        }

        public override Empty RemoveBeneficiary(RemoveBeneficiaryInput input)
        {
            Assert(input.SchemeId != null, "Invalid profit id.");
            Assert(input.Beneficiary != null, "Invalid Beneficiary address.");

            var profitItem = State.SchemeInfos[input.SchemeId];

            Assert(profitItem != null, "Profit item not found.");

            var currentDetail = State.ProfitDetailsMap[input.SchemeId][input.Beneficiary];

            if (profitItem == null || currentDetail == null)
            {
                return new Empty();
            }

            var expiryDetails = currentDetail.Details
                .Where(d => d.EndPeriod < profitItem.CurrentPeriod).ToList();

            if (!expiryDetails.Any())
            {
                return new Empty();
            }

            var shares = expiryDetails.Sum(d => d.Shares);
            foreach (var profitDetail in expiryDetails)
            {
                currentDetail.Details.Remove(profitDetail);
            }

            State.ProfitDetailsMap[input.SchemeId][input.Beneficiary] = currentDetail;

            // TODO: Recover this after key deletion in contract feature impled.
//            if (currentDetail.Details.Count != 0)
//            {
//                State.ProfitDetailsMap[input.SchemeId][input.Beneficiary] = currentDetail;
//            }
//            else
//            {
//                State.ProfitDetailsMap[input.SchemeId][input.Beneficiary] = null;
//            }

            profitItem.TotalShares -= shares;
            State.SchemeInfos[input.SchemeId] = profitItem;

            return new Empty();
        }

        public override Empty AddBeneficiaries(AddBeneficiariesInput input)
        {
            foreach (var beneficiaryShare in input.BeneficiaryShares)
            {
                AddBeneficiary(new AddBeneficiaryInput
                {
                    SchemeId = input.SchemeId,
                    BeneficiaryShare = beneficiaryShare,
                    EndPeriod = input.EndPeriod
                });
            }

            return new Empty();
        }

        public override Empty RemoveBeneficiaries(RemoveBeneficiariesInput input)
        {
            foreach (var beneficiary in input.Beneficiaries)
            {
                RemoveBeneficiary(new RemoveBeneficiaryInput
                {
                    SchemeId = input.SchemeId, Beneficiary = beneficiary
                });
            }

            return new Empty();
        }

        /// <summary>
        /// 
        /// Will burn/destroy a certain amount of profits if `input.Period` is less than 0.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty DistributeProfits(DistributeProfitsInput input)
        {
            Assert(input.Amount >= 0, "Amount must be greater than or equal to 0");

            Assert(input.Symbol != null && input.Symbol.Any(), "Invalid token symbol.");
            if (input.Symbol == null) return new Empty(); // Just to avoid IDE warning.

            var profitItem = State.SchemeInfos[input.SchemeId];
            Assert(profitItem != null, "Profit item not found.");
            if (profitItem == null) return new Empty(); // Just to avoid IDE warning.

            Assert(Context.Sender == profitItem.Creator, "Only creator can release profits.");

            var balance = AssertBalanceIsEnough(profitItem.VirtualAddress, input);

            if (profitItem.IsReleaseAllBalanceEverytimeByDefault && input.Amount == 0)
            {
                // Release all from general ledger.
                Context.LogDebug(() => $"Update releasing amount to {balance}");
                input.Amount = balance;
            }

            // Normally `input.TotalWeigh` should be 0, except the situation releasing of profits delayed for some reason.
            var totalShares = input.TotalShares == 0 ? profitItem.TotalShares : input.TotalShares;

            if (input.Period < 0 || totalShares <= 0)
            {
                return BurnProfits(input, profitItem, profitItem.VirtualAddress);
            }

            var releasingPeriod = profitItem.CurrentPeriod;
            Assert(input.Period == releasingPeriod,
                $"Invalid period. When release profit item {input.SchemeId.ToHex()} of period {input.Period}. Current period is {releasingPeriod}");

            var profitsReceivingVirtualAddress =
                GetReleasedPeriodProfitsVirtualAddress(profitItem.VirtualAddress, releasingPeriod);

            Context.LogDebug(() => $"Receiving virtual address: {profitsReceivingVirtualAddress}");

            var releasedProfitInformation = UpdateReleasedProfits(input, profitsReceivingVirtualAddress, totalShares);

            Context.LogDebug(() =>
                $"Released profit information of {input.SchemeId.ToHex()} in period {input.Period}, " +
                $"total Shares {releasedProfitInformation.TotalShares}, total amount {releasedProfitInformation.ProfitsAmount} {input.Symbol}s");

            PerformReleaseProfits(input, profitItem, totalShares, profitsReceivingVirtualAddress);

            profitItem.CurrentPeriod = input.Period.Add(1);
            profitItem.UndistributedProfits[input.Symbol] = profitItem.IsReleaseAllBalanceEverytimeByDefault
                ? 0
                : profitItem.UndistributedProfits[input.Symbol].Sub(input.Amount);

            State.SchemeInfos[input.SchemeId] = profitItem;

            return new Empty();
        }

        private long AssertBalanceIsEnough(Address virtualAddress, DistributeProfitsInput input)
        {
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Owner = virtualAddress,
                Symbol = input.Symbol
            }).Balance;
            Assert(input.Amount <= balance, "Insufficient profits amount.");

            return balance;
        }

        private Empty BurnProfits(DistributeProfitsInput input, Scheme scheme, Address profitVirtualAddress)
        {
            Context.LogDebug(() => "Entered BurnProfits.");
            scheme.CurrentPeriod = input.Period > 0 ? input.Period.Add(1) : scheme.CurrentPeriod;

            // Release to an address that no one can receive this amount of profits.
            if (input.Amount <= 0)
            {
                State.SchemeInfos[input.SchemeId] = scheme;
                return new Empty();
            }

            if (input.Period >= 0)
            {
                input.Period = -1;
            }

            // Burn this amount of profits.
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = profitVirtualAddress,
                To = Context.Self,
                Amount = input.Amount,
                Symbol = input.Symbol
            });
            State.TokenContract.Burn.Send(new BurnInput
            {
                Amount = input.Amount,
                Symbol = input.Symbol
            });
            scheme.UndistributedProfits[input.Symbol] =
                scheme.UndistributedProfits[input.Symbol].Sub(input.Amount);
            State.SchemeInfos[input.SchemeId] = scheme;
            return new Empty();
        }

        private DistributedProfitsInfo UpdateReleasedProfits(DistributeProfitsInput input,
            Address profitsReceivingVirtualAddress, long totalShares)
        {
            var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Owner = profitsReceivingVirtualAddress,
                Symbol = input.Symbol
            }).Balance;
            var releasedProfitInformation = State.ReleasedProfitsMap[profitsReceivingVirtualAddress];
            if (releasedProfitInformation == null)
            {
                releasedProfitInformation = new DistributedProfitsInfo
                {
                    TotalShares = totalShares,
                    ProfitsAmount = {{input.Symbol, input.Amount.Add(balance)}},
                    IsReleased = true
                };
            }
            else
            {
                // This means someone used `DistributeProfits` do donate to the specific account period of current profit item.
                releasedProfitInformation.TotalShares = totalShares;
                releasedProfitInformation.ProfitsAmount[input.Symbol] = balance.Add(input.Amount);
                releasedProfitInformation.IsReleased = true;
            }

            State.ReleasedProfitsMap[profitsReceivingVirtualAddress] = releasedProfitInformation;
            return releasedProfitInformation;
        }

        private void PerformReleaseProfits(DistributeProfitsInput input, Scheme scheme, long TotalShares,
            Address profitsReceivingVirtualAddress)
        {
            var remainAmount = input.Amount;

            remainAmount = ReleaseProfitsForSubProfitItems(input, scheme, TotalShares, remainAmount);

            // Transfer remain amount to individuals' receiving profits address.
            if (remainAmount != 0)
            {
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = scheme.VirtualAddress,
                    To = profitsReceivingVirtualAddress,
                    Amount = remainAmount,
                    Symbol = input.Symbol
                });
            }
        }

        private long ReleaseProfitsForSubProfitItems(DistributeProfitsInput input, Scheme scheme, long TotalShares,
            long remainAmount)
        {
            Context.LogDebug(() => $"Sub profit items count: {scheme.SubSchemes.Count}");
            foreach (var subProfitItem in scheme.SubSchemes)
            {
                Context.LogDebug(() => $"Releasing {subProfitItem.SchemeId}");

                // General ledger of this sub profit item.
                var subItemVirtualAddress = Context.ConvertVirtualAddressToContractAddress(subProfitItem.SchemeId);

                var amount = subProfitItem.Shares.Mul(input.Amount).Div(TotalShares);
                if (amount != 0)
                {
                    State.TokenContract.TransferFrom.Send(new TransferFromInput
                    {
                        From = scheme.VirtualAddress,
                        To = subItemVirtualAddress,
                        Amount = amount,
                        Symbol = input.Symbol
                    });
                }

                remainAmount -= amount;

                UpdateSubProfitItemInformation(input, subProfitItem, amount);

                // Update current_period of detail of sub profit item.
                var subItemDetail = State.ProfitDetailsMap[input.SchemeId][subItemVirtualAddress];
                foreach (var detail in subItemDetail.Details)
                {
                    detail.LastProfitPeriod = scheme.CurrentPeriod;
                }

                State.ProfitDetailsMap[input.SchemeId][subItemVirtualAddress] = subItemDetail;
            }

            return remainAmount;
        }

        private void UpdateSubProfitItemInformation(DistributeProfitsInput input, SchemeBeneficiaryShare subProfitItem, long amount)
        {
            var subItem = State.SchemeInfos[subProfitItem.SchemeId];
            if (subItem.UndistributedProfits.ContainsKey(input.Symbol))
            {
                subItem.UndistributedProfits[input.Symbol] =
                    subItem.UndistributedProfits[input.Symbol].Add(amount);
            }
            else
            {
                subItem.UndistributedProfits.Add(input.Symbol, amount);
            }

            State.SchemeInfos[subProfitItem.SchemeId] = subItem;
        }

        public override Empty ContributeProfits(ContributeProfitsInput input)
        {
            Assert(input.Symbol != null && input.Symbol.Any(), "Invalid token symbol.");
            if (input.Symbol == null) return new Empty(); // Just to avoid IDE warning.

            var profitItem = State.SchemeInfos[input.SchemeId];
            Assert(profitItem != null, "Profit item not found.");
            if (profitItem == null) return new Empty(); // Just to avoid IDE warning.

            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(input.SchemeId);

            if (input.Period == 0)
            {
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = Context.Sender,
                    To = virtualAddress,
                    Symbol = input.Symbol,
                    Amount = input.Amount,
                    Memo = $"Add {input.Amount} dividends for {input.SchemeId}."
                });
                if (!profitItem.UndistributedProfits.ContainsKey(input.Symbol))
                {
                    profitItem.UndistributedProfits.Add(input.Symbol, input.Amount);
                }
                else
                {
                    profitItem.UndistributedProfits[input.Symbol] =
                        profitItem.UndistributedProfits[input.Symbol].Add(input.Amount);
                }

                State.SchemeInfos[input.SchemeId] = profitItem;
            }
            else
            {
                var releasedProfitsVirtualAddress =
                    GetReleasedPeriodProfitsVirtualAddress(virtualAddress, input.Period);

                var releasedProfitsInformation = State.ReleasedProfitsMap[releasedProfitsVirtualAddress];
                if (releasedProfitsInformation == null)
                {
                    releasedProfitsInformation = new DistributedProfitsInfo
                    {
                        ProfitsAmount = {{input.Symbol, input.Amount}}
                    };
                }
                else
                {
                    Assert(!releasedProfitsInformation.IsReleased,
                        $"Profit item of period {input.Period} already released.");
                    releasedProfitsInformation.ProfitsAmount[input.Symbol] =
                        releasedProfitsInformation.ProfitsAmount[input.Symbol].Add(input.Amount);
                }

                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = Context.Sender,
                    To = releasedProfitsVirtualAddress,
                    Symbol = input.Symbol,
                    Amount = input.Amount,
                    Memo = $"Add dividends for {input.SchemeId} (period {input.Period})."
                });

                State.ReleasedProfitsMap[releasedProfitsVirtualAddress] = releasedProfitsInformation;
            }

            return new Empty();
        }
        
        /// <summary>
        /// Gain the profit form SchemeId from Details.lastPeriod to profitItem.currentPeriod-1;
        /// </summary>
        /// <param name="input">ClaimProfitsInput</param>
        /// <returns></returns>
        public override Empty ClaimProfits(ClaimProfitsInput input)
        {
            Assert(input.Symbol != null && input.Symbol.Any(), "Invalid token symbol.");
            if (input.Symbol == null) return new Empty(); // Just to avoid IDE warning.
            var profitItem = State.SchemeInfos[input.SchemeId];
            Assert(profitItem != null, "Profit item not found.");
            var profitDetails = State.ProfitDetailsMap[input.SchemeId][Context.Sender];
            Assert(profitDetails != null, "Profit details not found.");
            if (profitDetails == null || profitItem == null) return new Empty(); // Just to avoid IDE warning.

            Context.LogDebug(
                () => $"{Context.Sender} is trying to profit {input.Symbol} from {input.SchemeId.ToHex()}.");

            var profitVirtualAddress = Context.ConvertVirtualAddressToContractAddress(input.SchemeId);

            var availableDetails = profitDetails.Details.Where(d =>
                d.LastProfitPeriod < profitItem.CurrentPeriod && d.EndPeriod >= d.LastProfitPeriod
            ).ToList();

            Context.LogDebug(() =>
                $"Available details: {availableDetails.Aggregate("\n", (profit1, profit2) => profit1.ToString() + "\n" + profit2.ToString())}");

            // Only can get profit from last profit period to actual last period (profit.CurrentPeriod - 1),
            // because current period not released yet.
            for (var i = 0;
                i < Math.Min(ProfitContractConsts.ProfitReceivingLimitForEachTime, availableDetails.Count);
                i++)
            {
                var profitDetail = availableDetails[i];
                if (profitDetail.LastProfitPeriod == 0)
                {
                    // This detail never performed profit before.
                    profitDetail.LastProfitPeriod = profitDetail.StartPeriod;
                }

                ProfitAllPeriods(profitItem, input.Symbol, profitDetail, profitVirtualAddress);
            }

            State.ProfitDetailsMap[input.SchemeId][Context.Sender] = new ProfitDetails {Details = {availableDetails}};

            return new Empty();
        }

        private long ProfitAllPeriods(Scheme scheme, string symbol, ProfitDetail profitDetail,
            Address profitVirtualAddress, bool isView = false)
        {
            var totalAmount = 0L;
            var lastProfitPeriod = profitDetail.LastProfitPeriod;
            for (var period = profitDetail.LastProfitPeriod;
                period <= (profitDetail.EndPeriod == long.MaxValue
                    ? scheme.CurrentPeriod - 1
                    : Math.Min(scheme.CurrentPeriod - 1, profitDetail.EndPeriod));
                period++)
            {
                var periodToPrint = period;
                var detailToPrint = profitDetail;
                var releasedProfitsVirtualAddress =
                    GetReleasedPeriodProfitsVirtualAddress(profitVirtualAddress, period);
                var releasedProfitsInformation = State.ReleasedProfitsMap[releasedProfitsVirtualAddress];
                Context.LogDebug(() => $"Released profit information: {releasedProfitsInformation}");
                var amount = profitDetail.Shares.Mul(releasedProfitsInformation.ProfitsAmount[symbol])
                    .Div(releasedProfitsInformation.TotalShares);

                if (!isView)
                {
                    Context.LogDebug(() =>
                        $"{Context.Sender} is profiting {amount} {symbol} tokens from {scheme.SchemeId.ToHex()} in period {periodToPrint}." +
                        $"Sender's Shares: {detailToPrint.Shares}, total Shares: {releasedProfitsInformation.TotalShares}");
                    if (releasedProfitsInformation.IsReleased && amount > 0)
                    {
                        State.TokenContract.TransferFrom.Send(new TransferFromInput
                        {
                            From = releasedProfitsVirtualAddress,
                            To = Context.Sender,
                            Symbol = symbol,
                            Amount = amount
                        });
                    }

                    lastProfitPeriod = period + 1;
                }

                totalAmount = totalAmount.Add(amount);
            }

            profitDetail.LastProfitPeriod = lastProfitPeriod;

            return totalAmount;
        }
    }
}