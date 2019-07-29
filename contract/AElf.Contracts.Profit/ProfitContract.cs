using System;
using System.Linq;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContract : ProfitContractContainer.ProfitContractBase
    {

        /// <summary>
        /// Initialize a profit contract.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty InitializeProfitContract(Empty input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");

            State.Initialized.Value = true;

            return new Empty();
        }

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

            var tokenInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
            {
                Symbol = input.TokenSymbol
            });
            Assert(input.TokenSymbol != null && input.TokenSymbol.Any() && tokenInfo.TotalSupply != 0,
                "Invalid token symbol.");

            if (input.ExpiredPeriodNumber == 0)
            {
                input.ExpiredPeriodNumber = ProfitContractConsts.DefaultExpiredPeriodNumber;
            }

            var profitId = Context.TransactionId;
            var createdProfitIds = State.CreatedProfitItemsMap[Context.Sender]?.ProfitIds;
            if (createdProfitIds != null && createdProfitIds.Contains(profitId))
            {
                profitId = Hash.FromTwoHashes(profitId, createdProfitIds.Last());
            }

            State.ProfitItemsMap[profitId] = new ProfitItem
            {
                VirtualAddress = Context.ConvertVirtualAddressToContractAddress(profitId),
                Creator = Context.Sender,
                TokenSymbol = input.TokenSymbol,
                ExpiredPeriodNumber = input.ExpiredPeriodNumber,
                CurrentPeriod = 1,
                ReleaseAllIfAmountIsZero = input.ReleaseAllIfAmountIsZero
            };

            var createdProfitItems = State.CreatedProfitItemsMap[Context.Sender];
            if (createdProfitItems == null)
            {
                createdProfitItems = new CreatedProfitItems
                {
                    ProfitIds = {profitId}
                };
            }
            else
            {
                createdProfitItems.ProfitIds.Add(profitId);
            }

            State.CreatedProfitItemsMap[Context.Sender] = createdProfitItems;

            Context.LogDebug(() => $"Created profit item {profitId}");
            return profitId;
        }

        /// <summary>
        /// Register a SubProfitItem,binding to a ProfitItem as child.
        /// Then add the father ProfitItem's weight.
        /// </summary>
        /// <param name="input"></param>
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


            //binding the SubProfitItem to father ProfitItem
            profitItem.SubProfitItems.Add(new SubProfitItem
            {
                ProfitId = input.SubProfitId,
                Weight = input.SubItemWeight
            });
            //profitItem.TotalWeight = profitItem.TotalWeight.Add(input.SubItemWeight);
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

            Assert(input.EndPeriod >= profitItem.CurrentPeriod, "Invalid end period.");

            profitItem.TotalWeight = profitItem.TotalWeight.Add(input.Weight);

            State.ProfitItemsMap[profitId] = profitItem;

            var profitDetail = new ProfitDetail
            {
                StartPeriod = profitItem.CurrentPeriod,
                EndPeriod = input.EndPeriod,
                Weight = input.Weight,
            };

            //current profitContract receiver's profit details who gain the profit 
            var currentProfitDetails = State.ProfitDetailsMap[profitId][input.Receiver];
            if (currentProfitDetails == null)
            {
                // TODO: Reduce Resource token of Profit Contract from DApp Developer because this behaviour will add a new key.
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
                     d.EndPeriod.Add(profitItem.ExpiredPeriodNumber) < profitItem.CurrentPeriod))
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
        /// Higher level profit item has already released.
        /// Otherwise this profit item maybe has nothing to release.
        /// This pre-condition must be met before calling this method.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty ReleaseProfit(ReleaseProfitInput input)
        {
            Assert(input.Amount >= 0, $"Amount must greater than 0");
            var profitItem = State.ProfitItemsMap[input.ProfitId];

            Assert(profitItem != null, "Profit item not found.");

            if (profitItem == null)
            {
                return new Empty();
            }

            var totalWeight = input.TotalWeight == 0 ? profitItem.TotalWeight : input.TotalWeight;

            Assert(Context.Sender == profitItem.Creator, "Only creator can release profits.");

            var profitVirtualAddress = Context.ConvertVirtualAddressToContractAddress(input.ProfitId);

            var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Owner = profitVirtualAddress,
                Symbol = profitItem.TokenSymbol
            }).Balance;

            Assert(input.Amount <= balance, "Insufficient profits amount.");

            if (profitItem.ReleaseAllIfAmountIsZero && input.Amount == 0)
            {
                input.Amount = balance;
            }

            if (input.Period < 0 || totalWeight <= 0)
            {
                profitItem.CurrentPeriod = input.Period > 0 ? input.Period.Add(1) : profitItem.CurrentPeriod;

                // Release to an address no one can receive profits.
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
                    Symbol = profitItem.TokenSymbol
                });
                profitItem.TotalAmount = profitItem.TotalAmount.Sub(input.Amount);
                State.ProfitItemsMap[input.ProfitId] = profitItem;
                return new Empty();
            }

            // Update current_period.
            var releasingPeriod = profitItem.CurrentPeriod;

            Assert(input.Period == releasingPeriod,
                $"Invalid period. When release profit item {input.ProfitId.ToHex()} of period {input.Period}. Current period is {releasingPeriod}");

            //Compute the receivingVirtualAddress by profitVirtualAddress and releasingPeriod
            var profitsReceivingVirtualAddress =
                GetReleasedPeriodProfitsVirtualAddress(profitVirtualAddress, releasingPeriod);

            //ReleasedProfitsMap means the record that profit released at a period
            var releasedProfitInformation = State.ReleasedProfitsMap[profitsReceivingVirtualAddress];
            if (releasedProfitInformation == null)
            {
                releasedProfitInformation = new ReleasedProfitsInformation
                {
                    TotalWeight = totalWeight,
                    ProfitsAmount = input.Amount,
                    IsReleased = true
                };
            }
            else
            {
                releasedProfitInformation.TotalWeight = totalWeight;
                releasedProfitInformation.ProfitsAmount = releasedProfitInformation.ProfitsAmount.Add(input.Amount);
                releasedProfitInformation.IsReleased = true;
            }

            State.ReleasedProfitsMap[profitsReceivingVirtualAddress] = releasedProfitInformation;

            Context.LogDebug(() =>
                $"Released profit information of {input.ProfitId.ToHex()} in period {input.Period}, " +
                $"total weight {releasedProfitInformation.TotalWeight}, total amount {releasedProfitInformation.ProfitsAmount}");

            // Start releasing.

            var remainAmount = input.Amount;

            //according to the ratio that subProfitItem's weight divide totalWeight,subProfitItem can get the corresponding token.
            foreach (var subProfitItem in profitItem.SubProfitItems)
            {
                var subItemVirtualAddress = Context.ConvertVirtualAddressToContractAddress(subProfitItem.ProfitId);

                var amount = subProfitItem.Weight.Mul(input.Amount).Div(totalWeight);
                if (amount != 0)
                {
                    State.TokenContract.TransferFrom.Send(new TransferFromInput
                    {
                        From = profitVirtualAddress,
                        To = subItemVirtualAddress,
                        Amount = amount,
                        Symbol = profitItem.TokenSymbol
                    });
                }

                remainAmount -= amount;

                var subItem = State.ProfitItemsMap[subProfitItem.ProfitId];
                subItem.TotalAmount = subItem.TotalAmount.Add(amount);
                State.ProfitItemsMap[subProfitItem.ProfitId] = subItem;

                // Update current_period of detail of sub profit item.
                var subItemDetail = State.ProfitDetailsMap[input.ProfitId][subItemVirtualAddress];
                foreach (var detail in subItemDetail.Details)
                {
                    detail.LastProfitPeriod = profitItem.CurrentPeriod;
                }

                State.ProfitDetailsMap[input.ProfitId][subItemVirtualAddress] = subItemDetail;
            }

            // Transfer remain amount to individuals' receiving profits address.
            if (remainAmount != 0)
            {
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = profitVirtualAddress,
                    To = profitsReceivingVirtualAddress,
                    Amount = remainAmount,
                    Symbol = profitItem.TokenSymbol
                });
            }

            profitItem.CurrentPeriod = input.Period + 1;
            profitItem.TotalAmount -= input.Amount;
            State.ProfitItemsMap[input.ProfitId] = profitItem;

            return new Empty();
        }

        public override Empty AddProfits(AddProfitsInput input)
        {
            var profitItem = State.ProfitItemsMap[input.ProfitId];
            Assert(profitItem != null, "Profit item not found.");

            if (profitItem == null)
            {
                return new Empty();
            }

            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(input.ProfitId);

            //if input.period == 0,the token will transfer to the totalAmount in the profitItem
            //opposed,the token will transfer to the corresponding address of input.period
            if (input.Period == 0)
            {
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = Context.Sender,
                    To = virtualAddress,
                    Symbol = profitItem.TokenSymbol,
                    Amount = input.Amount,
                    Memo = $"Add dividends for {input.ProfitId}."
                });
                profitItem.TotalAmount += profitItem.TotalAmount.Add(input.Amount);
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
                        ProfitsAmount = input.Amount
                    };
                }
                else
                {
                    Assert(!releasedProfitsInformation.IsReleased,
                        $"Profit item of period {input.Period} already released.");
                    releasedProfitsInformation.ProfitsAmount =
                        releasedProfitsInformation.ProfitsAmount.Add(input.Amount);
                }

                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = Context.Sender,
                    To = releasedProfitsVirtualAddress,
                    Symbol = profitItem.TokenSymbol,
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
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty Profit(ProfitInput input)
        {
            Context.LogDebug(() => $"{Context.Sender} is trying to profit from {input.ProfitId.ToHex()}.");

            var profitItem = State.ProfitItemsMap[input.ProfitId];
            Assert(profitItem != null, "Profit item not found.");

            var profitDetails = State.ProfitDetailsMap[input.ProfitId][Context.Sender];

            Assert(profitDetails != null, "Profit details not found.");

            if (profitDetails == null || profitItem == null)
            {
                return new Empty();
            }

            var profitVirtualAddress = Context.ConvertVirtualAddressToContractAddress(input.ProfitId);

            var availableDetails = profitDetails.Details.Where(d => d.LastProfitPeriod != profitItem.CurrentPeriod)
                .ToList();

            for (var i = 0; i < Math.Min(ProfitContractConsts.ProfitLimit, availableDetails.Count); i++)
            {
                var profitDetail = availableDetails[i];
                if (profitDetail.LastProfitPeriod == 0)
                {
                    profitDetail.LastProfitPeriod = profitDetail.StartPeriod;
                }

                var lastProfitPeriod = profitDetail.LastProfitPeriod;
                //Can only get profit until profitItem.CurrentPeriod-1,because currentPeriod hasn't be released.
                for (var period = profitDetail.LastProfitPeriod;
                    period <= (profitDetail.EndPeriod == long.MaxValue
                        ? profitItem.CurrentPeriod - 1
                        : Math.Min(profitItem.CurrentPeriod - 1, profitDetail.EndPeriod));
                    period++)
                {
                    var releasedProfitsVirtualAddress =
                        GetReleasedPeriodProfitsVirtualAddress(profitVirtualAddress, period);
                    var releasedProfitsInformation = State.ReleasedProfitsMap[releasedProfitsVirtualAddress];
                    var amount = profitDetail.Weight.Mul(releasedProfitsInformation.ProfitsAmount)
                        .Div(releasedProfitsInformation.TotalWeight);
                    var period1 = period;
                    Context.LogDebug(() =>
                        $"{Context.Sender} is profiting {amount} tokens from {input.ProfitId.ToHex()} in period {period1}");
                    if (releasedProfitsInformation.IsReleased && amount > 0)
                    {
                        State.TokenContract.TransferFrom.Send(new TransferFromInput
                        {
                            From = releasedProfitsVirtualAddress,
                            To = Context.Sender,
                            Symbol = profitItem.TokenSymbol,
                            Amount = amount
                        });
                    }

                    lastProfitPeriod = period + 1;
                }

                profitDetail.LastProfitPeriod = lastProfitPeriod;
            }

            State.ProfitDetailsMap[input.ProfitId][Context.Sender] = profitDetails;

            return new Empty();
        }
    }
}