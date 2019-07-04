using System;
using System.Linq;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContract : ProfitContractContainer.ProfitContractBase
    {
        /// <summary>
        /// Create a ProfitItem
        /// At the first time,the profitItem's id is unknown,it may create by transaction id and createdProfitIds;
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Hash CreateProfitItem(CreateProfitItemInput input)
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

            var profitId = Context.TransactionId;
            // Why? Because one transaction may create many profit items via inline transactions.
            var createdProfitIds = State.CreatedProfitIds[Context.Sender]?.ProfitIds;
            if (createdProfitIds != null && createdProfitIds.Contains(profitId))
            {
                // So we choose this way to avoid profit id conflicts in aforementioned situation.
                profitId = Hash.FromTwoHashes(profitId, createdProfitIds.Last());
            }

            var profitItem = new ProfitItem
            {
                ProfitId = profitId,
                // The address of general ledger for current profit item.
                VirtualAddress = Context.ConvertVirtualAddressToContractAddress(profitId),
                Creator = Context.Sender,
                ProfitReceivingDuePeriodCount = input.ProfitReceivingDuePeriodCount,
                CurrentPeriod = 1,
                IsReleaseAllBalanceEverytimeByDefault = input.IsReleaseAllBalanceEveryTimeByDefault
            };
            State.ProfitItemsMap[profitId] = profitItem;

            var profitIds = State.CreatedProfitIds[Context.Sender];
            if (profitIds == null)
            {
                profitIds = new CreatedProfitIds
                {
                    ProfitIds = {profitId}
                };
            }
            else
            {
                profitIds.ProfitIds.Add(profitId);
            }

            State.CreatedProfitIds[Context.Sender] = profitIds;

            Context.LogDebug(() => $"Created profit item {State.ProfitItemsMap[profitId]}");

            Context.Fire(new ProfitItemCreated
            {
                ProfitId = profitItem.ProfitId,
                Creator = profitItem.Creator,
                IsReleaseAllBalanceEverytimeByDefault = profitItem.IsReleaseAllBalanceEverytimeByDefault,
                ProfitReceivingDuePeriodCount = profitItem.ProfitReceivingDuePeriodCount,
                VirtualAddress = profitItem.VirtualAddress
            });
            return profitId;
        }

        /// <summary>
        /// Register a SubProfitItem,binding to a ProfitItem as child.
        /// Then add the father ProfitItem's weight.
        /// </summary>
        /// <param name="input">RegisterSubProfitItemInput</param>
        /// <returns></returns>
        public override Empty RegisterSubProfitItem(RegisterSubProfitItemInput input)
        {
            Assert(input.ProfitId != input.SubProfitId, "Two profit items cannot be same.");
            Assert(input.SubItemWeight > 0, "Weight of sub profit item should greater than 0.");

            var profitItem = State.ProfitItemsMap[input.ProfitId];
            Assert(profitItem != null, "Profit item not found.");

            if (profitItem == null)
            {
                return new Empty();
            }

            Assert(Context.Sender == profitItem.Creator, "Only creator can do the registration.");

            var subProfitItemId = input.SubProfitId;
            var subProfitItem = State.ProfitItemsMap[subProfitItemId];
            Assert(subProfitItem != null, "Sub profit item not found.");

            if (subProfitItem == null)
            {
                return new Empty();
            }

            var subItemVirtualAddress = Context.ConvertVirtualAddressToContractAddress(subProfitItemId);
            AddWeight(new AddWeightInput
            {
                ProfitId = input.ProfitId,
                Weight = input.SubItemWeight,
                Receiver = subItemVirtualAddress,
                EndPeriod = long.MaxValue
            });

            // Add a sub profit item.
            profitItem.SubProfitItems.Add(new SubProfitItem
            {
                ProfitId = input.SubProfitId,
                Weight = input.SubItemWeight
            });
            profitItem.TotalWeight = profitItem.TotalWeight.Add(input.SubItemWeight);
            State.ProfitItemsMap[input.ProfitId] = profitItem;

            return new Empty();
        }

        public override Empty CancelSubProfitItem(CancelSubProfitItemInput input)
        {
            Assert(input.ProfitId != input.SubProfitId, "Two profit items cannot be same.");

            var profitItem = State.ProfitItemsMap[input.ProfitId];
            Assert(profitItem != null, "Profit item not found.");

            if (profitItem == null)
            {
                return new Empty();
            }

            Assert(input.SubItemCreator == profitItem.Creator, "Only creator can do the Cancel.");

            var subProfitItemId = input.SubProfitId;
            var subProfitItem = State.ProfitItemsMap[subProfitItemId];
            Assert(subProfitItem != null, "Sub profit item not found.");

            if (subProfitItem == null)
            {
                return new Empty();
            }

            var subItemVirtualAddress = Context.ConvertVirtualAddressToContractAddress(subProfitItemId);
            SubWeight(new SubWeightInput()
            {
                ProfitId = input.ProfitId,
                Receiver = subItemVirtualAddress
            });


            profitItem.SubProfitItems.Remove(profitItem.SubProfitItems.Single(d => d.ProfitId == input.SubProfitId));
            State.ProfitItemsMap[input.ProfitId] = profitItem;

            return new Empty();
        }

        public override Empty AddWeight(AddWeightInput input)
        {
            Assert(input.ProfitId != null, "Invalid profit id.");
            Assert(input.Receiver != null, "Invalid receiver address.");
            Assert(input.Weight >= 0, "Invalid weight.");

            if (input.EndPeriod == 0)
            {
                // Which means this profit receiver will never expired.
                input.EndPeriod = long.MaxValue;
            }

            var profitId = input.ProfitId;
            var profitItem = State.ProfitItemsMap[profitId];

            Assert(profitItem != null, "Profit item not found.");

            if (profitItem == null)
            {
                return new Empty();
            }

            Context.LogDebug(() =>
                $"{input.ProfitId}.\n End Period: {input.EndPeriod}, Current Period: {profitItem.CurrentPeriod}");

            Assert(input.EndPeriod >= profitItem.CurrentPeriod, "Invalid end period.");

            profitItem.TotalWeight = profitItem.TotalWeight.Add(input.Weight);

            State.ProfitItemsMap[profitId] = profitItem;

            var profitDetail = new ProfitDetail
            {
                StartPeriod = profitItem.CurrentPeriod,
                EndPeriod = input.EndPeriod,
                Weight = input.Weight,
            };

            var currentProfitDetails = State.ProfitDetailsMap[profitId][input.Receiver];
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

            State.ProfitDetailsMap[profitId][input.Receiver] = currentProfitDetails;

            Context.LogDebug(() => $"Add {input.Weight} weights to profit item {input.ProfitId.ToHex()}");

            return new Empty();
        }

        public override Empty SubWeight(SubWeightInput input)
        {
            Assert(input.ProfitId != null, "Invalid profit id.");
            Assert(input.Receiver != null, "Invalid receiver address.");

            var profitItem = State.ProfitItemsMap[input.ProfitId];

            Assert(profitItem != null, "Profit item not found.");

            var currentDetail = State.ProfitDetailsMap[input.ProfitId][input.Receiver];

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

            var weights = expiryDetails.Sum(d => d.Weight);
            foreach (var profitDetail in expiryDetails)
            {
                currentDetail.Details.Remove(profitDetail);
            }

            State.ProfitDetailsMap[input.ProfitId][input.Receiver] = currentDetail;

            // TODO: Recover this after key deletion in contract feature impled.
//            if (currentDetail.Details.Count != 0)
//            {
//                State.ProfitDetailsMap[input.ProfitId][input.Receiver] = currentDetail;
//            }
//            else
//            {
//                State.ProfitDetailsMap[input.ProfitId][input.Receiver] = null;
//            }

            profitItem.TotalWeight -= weights;
            State.ProfitItemsMap[input.ProfitId] = profitItem;

            return new Empty();
        }

        public override Empty AddWeights(AddWeightsInput input)
        {
            foreach (var map in input.Weights)
            {
                AddWeight(new AddWeightInput
                {
                    ProfitId = input.ProfitId,
                    Receiver = map.Receiver,
                    Weight = map.Weight,
                    EndPeriod = input.EndPeriod
                });
            }

            return new Empty();
        }

        public override Empty SubWeights(SubWeightsInput input)
        {
            foreach (var receiver in input.Receivers)
            {
                SubWeight(new SubWeightInput {ProfitId = input.ProfitId, Receiver = receiver});
            }

            return new Empty();
        }

        /// <summary>
        /// Will burn/destroy a certain amount of profits if `input.Period` is less than 0.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty ReleaseProfit(ReleaseProfitInput input)
        {
            Assert(input.Amount >= 0, "Amount must be greater than or equal to 0");

            Assert(input.Symbol != null && input.Symbol.Any(), "Invalid token symbol.");
            if (input.Symbol == null) return new Empty(); // Just to avoid IDE warning.

            var profitItem = State.ProfitItemsMap[input.ProfitId];
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
            var totalWeight = input.TotalWeight == 0 ? profitItem.TotalWeight : input.TotalWeight;

            if (input.Period < 0 || totalWeight <= 0)
            {
                return BurnProfits(input, profitItem, profitItem.VirtualAddress);
            }

            var releasingPeriod = profitItem.CurrentPeriod;
            Assert(input.Period == releasingPeriod,
                $"Invalid period. When release profit item {input.ProfitId.ToHex()} of period {input.Period}. Current period is {releasingPeriod}");

            var profitsReceivingVirtualAddress =
                GetReleasedPeriodProfitsVirtualAddress(profitItem.VirtualAddress, releasingPeriod);

            Context.LogDebug(() => $"Receiving virtual address: {profitsReceivingVirtualAddress}");

            var releasedProfitInformation = UpdateReleasedProfits(input, profitsReceivingVirtualAddress, totalWeight);

            Context.LogDebug(() =>
                $"Released profit information of {input.ProfitId.ToHex()} in period {input.Period}, " +
                $"total weight {releasedProfitInformation.TotalWeight}, total amount {releasedProfitInformation.ProfitsAmount} {input.Symbol}s");

            PerformReleaseProfits(input, profitItem, totalWeight, profitsReceivingVirtualAddress);

            profitItem.CurrentPeriod = input.Period.Add(1);
            profitItem.TotalAmounts[input.Symbol] = profitItem.IsReleaseAllBalanceEverytimeByDefault
                ? 0
                : profitItem.TotalAmounts[input.Symbol].Sub(input.Amount);

            State.ProfitItemsMap[input.ProfitId] = profitItem;

            return new Empty();
        }

        private long AssertBalanceIsEnough(Address virtualAddress, ReleaseProfitInput input)
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

        private Empty BurnProfits(ReleaseProfitInput input, ProfitItem profitItem, Address profitVirtualAddress)
        {
            Context.LogDebug(() => "Entered BurnProfits.");
            profitItem.CurrentPeriod = input.Period > 0 ? input.Period.Add(1) : profitItem.CurrentPeriod;

            // Release to an address that no one can receive this amount of profits.
            if (input.Amount <= 0)
            {
                State.ProfitItemsMap[input.ProfitId] = profitItem;
                return new Empty();
            }

            if (input.Period >= 0)
            {
                input.Period = -1;
            }

            // Which means the creator gonna burn this amount of profits.
            var profitsBurningVirtualAddress =
                GetReleasedPeriodProfitsVirtualAddress(profitVirtualAddress, input.Period);
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = profitVirtualAddress,
                To = profitsBurningVirtualAddress,
                Amount = input.Amount,
                Symbol = input.Symbol
            });
            profitItem.TotalAmounts[input.Symbol] =
                profitItem.TotalAmounts[input.Symbol].Sub(input.Amount);
            State.ProfitItemsMap[input.ProfitId] = profitItem;
            return new Empty();
        }

        private ReleasedProfitsInformation UpdateReleasedProfits(ReleaseProfitInput input,
            Address profitsReceivingVirtualAddress, long totalWeight)
        {
            var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Owner = profitsReceivingVirtualAddress,
                Symbol = input.Symbol
            }).Balance;
            var releasedProfitInformation = State.ReleasedProfitsMap[profitsReceivingVirtualAddress];
            if (releasedProfitInformation == null)
            {
                releasedProfitInformation = new ReleasedProfitsInformation
                {
                    TotalWeight = totalWeight,
                    ProfitsAmount = {{input.Symbol, input.Amount.Add(balance)}},
                    IsReleased = true
                };
            }
            else
            {
                // This means someone used `AddProfits` do donate to the specific account period of current profit item.
                releasedProfitInformation.TotalWeight = totalWeight;
                releasedProfitInformation.ProfitsAmount[input.Symbol] = balance.Add(input.Amount);
                releasedProfitInformation.IsReleased = true;
            }

            State.ReleasedProfitsMap[profitsReceivingVirtualAddress] = releasedProfitInformation;
            return releasedProfitInformation;
        }

        private void PerformReleaseProfits(ReleaseProfitInput input, ProfitItem profitItem, long totalWeight,
            Address profitsReceivingVirtualAddress)
        {
            var remainAmount = input.Amount;

            remainAmount = ReleaseProfitsForSubProfitItems(input, profitItem, totalWeight, remainAmount);

            // Transfer remain amount to individuals' receiving profits address.
            if (remainAmount != 0)
            {
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = profitItem.VirtualAddress,
                    To = profitsReceivingVirtualAddress,
                    Amount = remainAmount,
                    Symbol = input.Symbol
                });
            }
        }

        private long ReleaseProfitsForSubProfitItems(ReleaseProfitInput input, ProfitItem profitItem, long totalWeight,
            long remainAmount)
        {
            Context.LogDebug(() => $"Sub profit items count: {profitItem.SubProfitItems.Count}");
            foreach (var subProfitItem in profitItem.SubProfitItems)
            {
                Context.LogDebug(() => $"Releasing {subProfitItem.ProfitId}");

                // General ledger of this sub profit item.
                var subItemVirtualAddress = Context.ConvertVirtualAddressToContractAddress(subProfitItem.ProfitId);

                var amount = subProfitItem.Weight.Mul(input.Amount).Div(totalWeight);
                if (amount != 0)
                {
                    State.TokenContract.TransferFrom.Send(new TransferFromInput
                    {
                        From = profitItem.VirtualAddress,
                        To = subItemVirtualAddress,
                        Amount = amount,
                        Symbol = input.Symbol
                    });
                }

                remainAmount -= amount;

                UpdateSubProfitItemInformation(input, subProfitItem, amount);

                // Update current_period of detail of sub profit item.
                var subItemDetail = State.ProfitDetailsMap[input.ProfitId][subItemVirtualAddress];
                foreach (var detail in subItemDetail.Details)
                {
                    detail.LastProfitPeriod = profitItem.CurrentPeriod;
                }

                State.ProfitDetailsMap[input.ProfitId][subItemVirtualAddress] = subItemDetail;
            }

            return remainAmount;
        }

        private void UpdateSubProfitItemInformation(ReleaseProfitInput input, SubProfitItem subProfitItem, long amount)
        {
            var subItem = State.ProfitItemsMap[subProfitItem.ProfitId];
            if (subItem.TotalAmounts.ContainsKey(input.Symbol))
            {
                subItem.TotalAmounts[input.Symbol] =
                    subItem.TotalAmounts[input.Symbol].Add(amount);
            }
            else
            {
                subItem.TotalAmounts.Add(input.Symbol, amount);
            }

            State.ProfitItemsMap[subProfitItem.ProfitId] = subItem;
        }

        public override Empty AddProfits(AddProfitsInput input)
        {
            Assert(input.Symbol != null && input.Symbol.Any(), "Invalid token symbol.");
            if (input.Symbol == null) return new Empty(); // Just to avoid IDE warning.

            var profitItem = State.ProfitItemsMap[input.ProfitId];
            Assert(profitItem != null, "Profit item not found.");
            if (profitItem == null) return new Empty(); // Just to avoid IDE warning.

            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(input.ProfitId);

            if (input.Period == 0)
            {
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = Context.Sender,
                    To = virtualAddress,
                    Symbol = input.Symbol,
                    Amount = input.Amount,
                    Memo = $"Add {input.Amount} dividends for {input.ProfitId}."
                });
                if (!profitItem.TotalAmounts.ContainsKey(input.Symbol))
                {
                    profitItem.TotalAmounts.Add(input.Symbol, input.Amount);
                }
                else
                {
                    profitItem.TotalAmounts[input.Symbol] =
                        profitItem.TotalAmounts[input.Symbol].Add(input.Amount);
                }

                State.ProfitItemsMap[input.ProfitId] = profitItem;
            }
            else
            {
                var releasedProfitsVirtualAddress =
                    GetReleasedPeriodProfitsVirtualAddress(virtualAddress, input.Period);

                var releasedProfitsInformation = State.ReleasedProfitsMap[releasedProfitsVirtualAddress];
                if (releasedProfitsInformation == null)
                {
                    releasedProfitsInformation = new ReleasedProfitsInformation
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
                    Memo = $"Add dividends for {input.ProfitId} (period {input.Period})."
                });

                State.ReleasedProfitsMap[releasedProfitsVirtualAddress] = releasedProfitsInformation;
            }

            return new Empty();
        }

        /// <summary>
        /// Gain the profit form profitId from Details.lastPeriod to profitItem.currentPeriod-1;
        /// </summary>
        /// <param name="input">ProfitInput</param>
        /// <returns></returns>
        public override Empty Profit(ProfitInput input)
        {
            Assert(input.Symbol != null && input.Symbol.Any(), "Invalid token symbol.");
            if (input.Symbol == null) return new Empty(); // Just to avoid IDE warning.
            var profitItem = State.ProfitItemsMap[input.ProfitId];
            Assert(profitItem != null, "Profit item not found.");
            var profitDetails = State.ProfitDetailsMap[input.ProfitId][Context.Sender];
            Assert(profitDetails != null, "Profit details not found.");
            if (profitDetails == null || profitItem == null) return new Empty(); // Just to avoid IDE warning.

            Context.LogDebug(
                () => $"{Context.Sender} is trying to profit {input.Symbol} from {input.ProfitId.ToHex()}.");

            var profitVirtualAddress = Context.ConvertVirtualAddressToContractAddress(input.ProfitId);

            var availableDetails = profitDetails.Details.Where(d => d.LastProfitPeriod < profitItem.CurrentPeriod)
                .ToList();

            // Only can get profit from last profit period to actual last period (profit.CurrentPeriod - 1),
            // because current period not released yet.
            for (var i = 0;
                i < Math.Min(ProfitContractConsts.ProfitReceivingLimitForEachTime, availableDetails.Count);
                i++)
            {
                var profitDetail = availableDetails[i];
                if (profitDetail.LastProfitPeriod == 0)
                {
                    profitDetail.LastProfitPeriod = profitDetail.StartPeriod;
                }

                ProfitAllPeriods(input, profitDetail, profitItem, profitVirtualAddress);
            }

            State.ProfitDetailsMap[input.ProfitId][Context.Sender] = profitDetails;

            return new Empty();
        }

        private void ProfitAllPeriods(ProfitInput input, ProfitDetail profitDetail, ProfitItem profitItem,
            Address profitVirtualAddress)
        {
            var lastProfitPeriod = profitDetail.LastProfitPeriod;
            for (var period = profitDetail.LastProfitPeriod;
                period <= (profitDetail.EndPeriod == long.MaxValue
                    ? profitItem.CurrentPeriod - 1
                    : Math.Min(profitItem.CurrentPeriod - 1, profitDetail.EndPeriod));
                period++)
            {
                var releasedProfitsVirtualAddress =
                    GetReleasedPeriodProfitsVirtualAddress(profitVirtualAddress, period);
                var releasedProfitsInformation = State.ReleasedProfitsMap[releasedProfitsVirtualAddress];
                var amount = profitDetail.Weight.Mul(releasedProfitsInformation.ProfitsAmount[input.Symbol])
                    .Div(releasedProfitsInformation.TotalWeight);
                var periodToPrint = period;
                Context.LogDebug(() =>
                    $"{Context.Sender} is profiting {amount} tokens from {input.ProfitId.ToHex()} in period {periodToPrint}");
                if (releasedProfitsInformation.IsReleased && amount > 0)
                {
                    State.TokenContract.TransferFrom.Send(new TransferFromInput
                    {
                        From = releasedProfitsVirtualAddress,
                        To = Context.Sender,
                        Symbol = input.Symbol,
                        Amount = amount
                    });
                }

                lastProfitPeriod = period + 1;
            }

            profitDetail.LastProfitPeriod = lastProfitPeriod;
        }
    }
}