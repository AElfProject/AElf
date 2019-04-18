using System;
using System.Linq;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContract : ProfitContractContainer.ProfitContractBase
    {
        public override Empty InitializeProfitContract(InitializeProfitContractInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");

            State.TokenContractSystemName.Value = input.TokenContractSystemName;

            State.Initialized.Value = true;

            return new Empty();
        }

        public override Hash CreateProfitItem(CreateProfitItemInput input)
        {
            var profitId = Hash.FromRawBytes(Context.Self.Value.Concat(Context.TransactionId).ToArray());
            State.ProfitItemsMap[profitId] = new ProfitItem
            {
                IsSubProfitItem = input.IsSubProfitItem,
                TokenSymbol = input.TokenSymbol,
                CurrentPeriod = 1
            };
            return profitId;
        }

        public override Empty RegisterSubProfitItem(RegisterSubProfitItemInput input)
        {
            var profitItem = State.ProfitItemsMap[input.ProfitId];
            Assert(profitItem != null, "Profit item not found.");

            if (profitItem == null)
            {
                return new Empty();
            }
            
            // TODO: One of `Treasury`'s Send Address is `Voters`' Receive Address
            var subProfitItemId = input.SubProfitId;
            var subProfitItem = State.ProfitItemsMap[subProfitItemId];
            Assert(subProfitItem != null, "Sub profit item not found.");

            if (subProfitItem == null)
            {
                return new Empty();
            }

            Assert(subProfitItem.IsSubProfitItem, "Cannot register sub profit item.");

            Assert(!State.RegisterMap[input.ProfitId][input.SubProfitId], "Sub profit item already registered.");

            State.RegisterMap[input.ProfitId][input.SubProfitId] = true;

            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(subProfitItemId);
            AddWeight(new AddWeightInput
            {
                ItemName = input.ItemName,
                Weight = input.SubItemWeight,
                Receiver = virtualAddress
            });

            profitItem.SubProfitItems.Add(new SubProfitItem
            {
                Creator = input.SubItemCreator,
                ItemName = input.SubItemName,
                Weight = input.SubItemWeight
            });
            State.ProfitItemsMap[input.ProfitId] = profitItem;

            return new Empty();
        }

        public override Empty AddWeight(AddWeightInput input)
        {
            // TODO: Use profit id directly.
            Assert(input.Weight >= 0, "Invalid weight.");

            var profitId = input.ProfitId;
            var profitItem = State.ProfitItemsMap[profitId];

            Assert(profitItem != null, "Profit item not found.");

            if (profitItem == null)
            {
                return new Empty();
            }

            profitItem.TotalWeight += input.Weight;

            State.ProfitItemsMap[profitId] = profitItem;

            if (input.EndPeriod == 0)
            {
                // Which means this profit receiver will never expired.
                input.EndPeriod = long.MaxValue;
            }

            var profitDetail = new ProfitDetail
            {
                StartPeriod = profitItem.CurrentPeriod,
                EndPeriod = input.EndPeriod,
                Weight = input.Weight
            };
            var currentProfitDetails = State.ProfitDetailsMap[profitId][input.Receiver];
            if (currentProfitDetails == null)
            {
                // TODO: Deduct Resource token of Profit Contract from DAPP Developer because this behaviour will add a new key.
                currentProfitDetails = new ProfitDetails
                {
                    Details = {profitDetail}
                };
            }
            else
            {
                currentProfitDetails.Details.Add(profitDetail);
            }
            
            // TODO: Remove expired details.

            State.ProfitDetailsMap[profitId][input.Receiver] = currentProfitDetails;

            return new Empty();
        }

        public override Empty SubWeight(SubWeightInput input)
        {
            var profitItem = State.ProfitItemsMap[input.ProfitId];

            Assert(profitItem != null, "Profit item not found.");

            var currentDetail = State.ProfitDetailsMap[input.ProfitId][input.Receiver];

            Assert(currentDetail != null, "Profit detail not found.");

            if (currentDetail == null || profitItem == null)
            {
                return new Empty();
            }

            var detailsNeedToRemove =
                currentDetail.Details.Where(d => d.EndPeriod <= profitItem.CurrentPeriod).ToList();

            if (!detailsNeedToRemove.Any())
            {
                return new Empty();
            }

            var weights = detailsNeedToRemove.Sum(d => d.Weight);
            foreach (var profitDetail in detailsNeedToRemove)
            {
                currentDetail.Details.Remove(profitDetail);
            }

            State.ProfitDetailsMap[input.ProfitId][input.Receiver] = currentDetail;

            profitItem.TotalWeight -= weights;
            State.ProfitItemsMap[input.ProfitId] = profitItem;

            return new Empty();
        }

        public override Empty ReleaseProfit(ReleaseProfitInput input)
        {
            var profitItem = State.ProfitItemsMap[input.ProfitId];

            Assert(profitItem != null, "Profit item not found.");

            if (profitItem == null)
            {
                return new Empty();
            }
            
            Assert(profitItem.TotalWeight > 0, "Invalid total weight.");

            Assert(input.Amount <= profitItem.TotalAmount, "Insufficient profits amount.");

            var salt = GetReleasedPeriodProfitsVirtualAddressSalt(input.ProfitId, input.Period);
            // TODO: Use map to restore amount of each period.
            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(salt);
            Context.SendVirtualInline(salt, State.TokenContract.Value, "Transfer", new TransferInput
            {
                To = virtualAddress,
                Symbol = profitItem.TokenSymbol,
                Amount = input.Amount,
                Memo = $"Release dividends for {input.ItemName} (period {input.Period})."
            }.ToByteString());

            State.PeriodWeightsMap[virtualAddress] = profitItem.TotalWeight;

            foreach (var subProfitItem in profitItem.SubProfitItems)
            {
                var subSalt = GetReleasedPeriodProfitsVirtualAddressSalt(subProfitItem.ProfitId, input.Period);
                var subProfitItemVirtualAddress = Context.ConvertVirtualAddressToContractAddress(subSalt);

                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = virtualAddress,
                    To = subProfitItemVirtualAddress,
                    Amount = subProfitItem.Weight.Mul(input.Amount).Div(profitItem.TotalWeight),
                    Symbol = profitItem.TokenSymbol
                });
            }

            return new Empty();
        }

        public override Empty AddDividends(AddDividendsInput input)
        {
            var profitItem = State.ProfitItemsMap[input.ProfitId];
            Assert(profitItem != null, "Profit item not found.");

            if (profitItem == null)
            {
                return new Empty();
            }

            var salt = GetReleasedPeriodProfitsVirtualAddressSalt(input.ProfitId, input.Period);
            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(salt);
            Context.SendVirtualInline(salt, State.TokenContract.Value, "TransferFrom", new TransferFromInput
            {
                From = Context.Sender,
                To = virtualAddress,
                Symbol = profitItem.TokenSymbol,
                Amount = input.Amount,
                Memo = $"Add dividends for {input.ProfitId} (period {input.Period})."
            }.ToByteString());

            return new Empty();
        }

        public override Empty Profit(ProfitInput input)
        {
            var profitItem = State.ProfitItemsMap[input.ProfitId];
            Assert(profitItem != null, "Profit item not found.");

            var profitDetails = State.ProfitDetailsMap[input.ProfitId][Context.Sender];

            Assert(profitDetails != null, "Profit details not found.");

            if (profitDetails == null || profitItem == null)
            {
                return new Empty();
            }

            for (var i = 0; i < Math.Min(ProfitContractConsts.ProfitLimit, profitDetails.Details.Count); i++)
            {
                var profitDetail = profitDetails.Details[i];
                if (profitDetail.LastProfitPeriod == 0)
                {
                    profitDetail.LastProfitPeriod = profitDetail.StartPeriod;
                }

                for (var period = profitDetail.LastProfitPeriod; period < profitItem.CurrentPeriod; period++)
                {
                    var salt = GetReleasedPeriodProfitsVirtualAddressSalt(input.ProfitId, period);
                    var virtualAddress = Context.ConvertVirtualAddressToContractAddress(salt);
                    var totalWeights = State.PeriodWeightsMap[virtualAddress];
                    var totalProfits = State.TokenContract.GetBalance.Call(new GetBalanceInput
                    {
                        Owner = virtualAddress,
                        Symbol = profitItem.TokenSymbol
                    }).Balance;
                    State.TokenContract.TransferFrom.Send(new TransferFromInput
                    {
                        From = virtualAddress,
                        To = Context.Sender,
                        Symbol = profitItem.TokenSymbol,
                        Amount = profitDetail.Weight.Mul(totalProfits).Div(totalWeights)
                    });
                }
            }

            return new Empty();
        }

        private Hash GetReleasedPeriodProfitsVirtualAddressSalt(Hash profitId, long period)
        {
            return Hash.FromRawBytes(profitId.Value.Concat(period.ToString().CalculateHash()).ToArray());
        }
    }
}