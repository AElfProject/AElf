using System.Linq;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
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
            var profitId = GetProfitId(input.Creator, input.ItemName);
            Assert(State.ProfitItemsMap[profitId] != null, "Profit item already exists.");
            State.ProfitItemsMap[profitId] = new ProfitItem
            {
                Creator = input.Creator,
                ItemName = input.ItemName,
                ProfitId = GetProfitId(input.Creator, input.ItemName),
                InitialWeight = input.IsTotalWeightFixed ? input.InitialWeight : 0,
                IsTotalWeightFixed = input.IsTotalWeightFixed,
                IsSubProfitItem = input.IsSubProfitItem
            };
            return profitId;
        }

        public override Empty RegisterSubProfitItem(RegisterSubProfitItemInput input)
        {
            var profitItemId = GetProfitId(Context.Sender, input.ItemName);
            var profitItem = State.ProfitItemsMap[profitItemId];
            Assert(profitItem != null, "Profit item not found.");

            if (profitItem == null)
            {
                return new Empty();
            }
            
            var subProfitItemId = GetProfitId(input.SubItemCreator, input.SubItemName);
            var subProfitItem = State.ProfitItemsMap[subProfitItemId];
            Assert(subProfitItem != null, "Sub profit item not found.");

            if (subProfitItem == null)
            {
                return new Empty();
            }

            Assert(subProfitItem.IsSubProfitItem, "Cannot register sub profit item.");

            Assert(!State.RegisterMap[profitItemId][subProfitItemId], "Sub profit item already registered.");

            State.RegisterMap[profitItemId][subProfitItemId] = true;

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
            State.ProfitItemsMap[profitItemId] = profitItem;

            return new Empty();
        }

        public override Empty AddWeight(AddWeightInput input)
        {
            Assert(input.Weight >= 0, "Invalid weight.");

            var profitId = GetProfitId(Context.Sender, input.ItemName);
            var profitItem = State.ProfitItemsMap[profitId];

            Assert(profitItem != null, "Profit item not found.");

            if (profitItem == null)
            {
                return new Empty();
            }

            profitItem.TotalWeight += input.Weight;

            if (profitItem.IsTotalWeightFixed)
            {
                Assert(profitItem.TotalWeight <= profitItem.InitialWeight,
                    "Total weight cannot exceed initial weight if total weight fixed.");
            }

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
                currentProfitDetails = new ProfitDetails
                {
                    Details = {profitDetail}
                };
            }
            else
            {
                currentProfitDetails.Details.Add(profitDetail);
            }

            State.ProfitDetailsMap[profitId][input.Receiver] = currentProfitDetails;

            return new Empty();
        }

        public override Empty SubWeight(SubWeightInput input)
        {
            var profitId = GetProfitId(Context.Sender, input.ItemName);
            var profitItem = State.ProfitItemsMap[profitId];

            Assert(profitItem != null, "Profit item not found.");

            var currentDetail = State.ProfitDetailsMap[profitId][input.Receiver];

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

            State.ProfitDetailsMap[profitId][input.Receiver] = currentDetail;

            profitItem.TotalWeight -= weights;
            State.ProfitItemsMap[profitId] = profitItem;

            return new Empty();
        }

        public override Empty ReleaseProfit(ReleaseProfitInput input)
        {
            var profitId = GetProfitId(Context.Sender, input.ItemName);
            var profitItem = State.ProfitItemsMap[profitId];

            Assert(profitItem != null, "Profit item not found.");

            if (profitItem == null)
            {
                return new Empty();
            }
            
            Assert(profitItem.TotalWeight > 0, "Invalid total weight.");

            Assert(input.Amount <= profitItem.TotalAmount, "Insufficient profits amount.");

            var salt = GetReleasedPeriodProfitsVirtualAddressSalt(profitId, input.Period);
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
                var subProfitItemId = GetProfitId(subProfitItem.Creator, subProfitItem.ItemName);
                var subSalt = GetReleasedPeriodProfitsVirtualAddressSalt(subProfitItemId, input.Period);
                var subProfitItemVirtualAddress = Context.ConvertVirtualAddressToContractAddress(subSalt);

                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = virtualAddress,
                    To = subProfitItemVirtualAddress,
                    Amount = subProfitItem.Weight * input.Amount / profitItem.TotalWeight,
                    Symbol = profitItem.TokenSymbol
                });
            }

            return new Empty();
        }

        public override Empty AddDividends(AddDividendsInput input)
        {
            var profitId = GetProfitId(input.Creator, input.ItemName);
            var profitItem = State.ProfitItemsMap[profitId];
            Assert(profitItem != null, "Profit item not found.");

            if (profitItem == null)
            {
                return new Empty();
            }

            var salt = GetReleasedPeriodProfitsVirtualAddressSalt(profitId, input.Period);
            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(salt);
            Context.SendVirtualInline(salt, State.TokenContract.Value, "TransferFrom", new TransferFromInput
            {
                From = Context.Sender,
                To = virtualAddress,
                Symbol = profitItem.TokenSymbol,
                Amount = input.Amount,
                Memo = $"Add dividends for {input.ItemName} (period {input.Period}) created by {input.Creator}."
            }.ToByteString());

            return new Empty();
        }

        public override Empty Profit(ProfitInput input)
        {
            var profitId = GetProfitId(input.Creator, input.ItemName);
            var profitItem = State.ProfitItemsMap[profitId];
            Assert(profitItem != null, "Profit item not found.");

            var profitDetails = State.ProfitDetailsMap[profitId][Context.Sender];

            Assert(profitDetails != null, "Profit details not found.");

            if (profitDetails == null || profitItem == null)
            {
                return new Empty();
            }

            foreach (var profitDetail in profitDetails.Details)
            {
                if (profitDetail.LastProfitPeriod == 0)
                {
                    profitDetail.LastProfitPeriod = profitDetail.StartPeriod;
                }

                for (var period = profitDetail.LastProfitPeriod; period < profitItem.CurrentPeriod; period++)
                {
                    var salt = GetReleasedPeriodProfitsVirtualAddressSalt(profitId, period);
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
                        Amount = profitDetail.Weight * totalProfits / totalWeights
                    });
                }
            }

            return new Empty();
        }

        private Hash GetProfitId(Address creator, string itemName)
        {
            return Hash.FromRawBytes(creator.Value.Concat(itemName.CalculateHash()).ToArray());
        }

        private Hash GetReleasedPeriodProfitsVirtualAddressSalt(Hash profitId, long period)
        {
            return Hash.FromRawBytes(profitId.Value.Concat(period.ToString().CalculateHash()).ToArray());
        }
    }
}