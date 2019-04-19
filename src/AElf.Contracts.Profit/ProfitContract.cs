using System;
using System.Linq;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Crypto.Tls;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContract : ProfitContractContainer.ProfitContractBase
    {
        public override Empty InitializeProfitContract(InitializeProfitContractInput input)
        {
            Assert(Context.Sender == Context.GetZeroSmartContractAddress(),
                "Only zero contract can initialize this contract.");

            Assert(!State.Initialized.Value, "Already initialized.");
            State.Initialized.Value = true;

            State.TokenContractSystemName.Value = input.TokenContractSystemName;

            return new Empty();
        }

        public override Hash CreateProfitItem(CreateProfitItemInput input)
        {
            if (State.TokenContract.Value == null)
            {
                State.BasicContractZero.Value = Context.GetZeroSmartContractAddress();
                State.TokenContract.Value =
                    State.BasicContractZero.GetContractAddressByName.Call(State.TokenContractSystemName.Value);
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
            State.ProfitItemsMap[profitId] = new ProfitItem
            {
                VirtualAddress = Context.ConvertVirtualAddressToContractAddress(profitId),
                Creator = Context.Sender,
                TokenSymbol = input.TokenSymbol,
                ExpiredPeriodNumber = input.ExpiredPeriodNumber,
                CurrentPeriod = 1
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
            return profitId;
        }

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

            profitItem.SubProfitItems.Add(new SubProfitItem
            {
                ProfitId = input.SubProfitId,
                Weight = input.SubItemWeight
            });
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

            profitItem.TotalWeight += input.Weight;

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
            foreach (var detail in currentProfitDetails.Details.Where(d =>
                d.EndPeriod + profitItem.ExpiredPeriodNumber < profitItem.CurrentPeriod))
            {
                currentProfitDetails.Details.Remove(detail);
            }

            State.ProfitDetailsMap[profitId][input.Receiver] = currentProfitDetails;

            return new Empty();
        }

        public override Empty SubWeight(SubWeightInput input)
        {
            Assert(input.ProfitId != null, "Invalid profit id.");
            Assert(input.Receiver != null, "Invalid receiver address.");

            var profitItem = State.ProfitItemsMap[input.ProfitId];

            Assert(profitItem != null, "Profit item not found.");

            var currentDetail = State.ProfitDetailsMap[input.ProfitId][input.Receiver];

            Assert(currentDetail != null, "Profit detail not found.");

            if (profitItem == null || currentDetail == null)
            {
                return new Empty();
            }

            var expiredDetails = currentDetail.Details
                .Where(d => d.EndPeriod < profitItem.CurrentPeriod && d.LastProfitPeriod == d.EndPeriod).ToList();

            if (!expiredDetails.Any())
            {
                return new Empty();
            }

            var weights = expiredDetails.Sum(d => d.Weight);
            foreach (var profitDetail in expiredDetails)
            {
                currentDetail.Details.Remove(profitDetail);
            }

            State.ProfitDetailsMap[input.ProfitId][input.Receiver] = currentDetail;

            profitItem.TotalWeight -= weights;
            State.ProfitItemsMap[input.ProfitId] = profitItem;

            return new Empty();
        }

        /// <summary>
        /// There should be at least one pre-condition to release profits if this is a sub profit item:
        /// Higher level profit item has already released.
        /// Otherwise this profit item maybe has nothing to release.
        /// This pre-condition should be met before calling this method.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty ReleaseProfit(ReleaseProfitInput input)
        {
            var profitItem = State.ProfitItemsMap[input.ProfitId];

            Assert(profitItem != null, "Profit item not found.");

            if (profitItem == null)
            {
                return new Empty();
            }

            // Update current_period.
            profitItem.CurrentPeriod += 1;
            State.ProfitItemsMap[input.ProfitId] = profitItem;

            Assert(profitItem.TotalWeight > 0, "Invalid total weight.");

            var profitVirtualAddress = Context.ConvertVirtualAddressToContractAddress(input.ProfitId);

            var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Owner = profitVirtualAddress,
                Symbol = profitItem.TokenSymbol
            }).Balance;

            Assert(input.Amount <= balance, "Insufficient profits amount.");

            var profitsReceivingVirtualAddress =
                GetReleasedPeriodProfitsVirtualAddress(profitVirtualAddress, input.Period);

            var releasedProfitInformation = State.ReleasedProfitsMap[profitsReceivingVirtualAddress];
            if (releasedProfitInformation == null)
            {
                releasedProfitInformation = new ReleasedProfitsInformation
                {
                    TotalWeight = profitItem.TotalWeight,
                    ProfitsAmount = input.Amount,
                    IsReleased = true
                };
            }
            else
            {
                releasedProfitInformation.TotalWeight = profitItem.TotalWeight;
                releasedProfitInformation.ProfitsAmount += input.Amount;
                releasedProfitInformation.IsReleased = true;
            }

            State.ReleasedProfitsMap[profitsReceivingVirtualAddress] = releasedProfitInformation;

            foreach (var subProfitItem in profitItem.SubProfitItems)
            {
                var subItemVirtualAddress = Context.ConvertVirtualAddressToContractAddress(subProfitItem.ProfitId);

                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = profitsReceivingVirtualAddress,
                    To = subItemVirtualAddress,
                    Amount = subProfitItem.Weight.Mul(input.Amount).Div(profitItem.TotalWeight),
                    Symbol = profitItem.TokenSymbol
                });

                var subItemDetail = State.ProfitDetailsMap[input.ProfitId][subItemVirtualAddress];
                foreach (var detail in subItemDetail.Details)
                {
                    detail.LastProfitPeriod = profitItem.CurrentPeriod;
                }

                State.ProfitDetailsMap[input.ProfitId][subItemVirtualAddress] = subItemDetail;
            }

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
                profitItem.TotalAmount += input.Amount;
                State.ProfitItemsMap[input.ProfitId] = profitItem;
            }
            else
            {
                var releasedProfitsVirtualAddress = GetReleasedPeriodProfitsVirtualAddress(virtualAddress, input.Period);
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = Context.Sender,
                    To = releasedProfitsVirtualAddress,
                    Symbol = profitItem.TokenSymbol,
                    Amount = input.Amount,
                    Memo = $"Add dividends for {input.ProfitId} (period {input.Period})."
                });
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
                    releasedProfitsInformation.ProfitsAmount += input.Amount;
                }
                State.ReleasedProfitsMap[releasedProfitsVirtualAddress] = releasedProfitsInformation;
            }

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

            var profitVirtualAddress = Context.ConvertVirtualAddressToContractAddress(input.ProfitId);

            for (var i = 0; i < Math.Min(ProfitContractConsts.ProfitLimit, profitDetails.Details.Count); i++)
            {
                var profitDetail = profitDetails.Details[i];
                if (profitDetail.LastProfitPeriod == 0)
                {
                    profitDetail.LastProfitPeriod = profitDetail.StartPeriod;
                }

                for (var period = profitDetail.LastProfitPeriod;
                    period < Math.Min(profitItem.CurrentPeriod, profitDetail.EndPeriod + 1);
                    period++)
                {
                    var releasedProfitsVirtualAddress = GetReleasedPeriodProfitsVirtualAddress(profitVirtualAddress, period);
                    var releasedProfitsInformation = State.ReleasedProfitsMap[releasedProfitsVirtualAddress];
                    if (releasedProfitsInformation.IsReleased)
                    {
                        State.TokenContract.TransferFrom.Send(new TransferFromInput
                        {
                            From = releasedProfitsVirtualAddress,
                            To = Context.Sender,
                            Symbol = profitItem.TokenSymbol,
                            Amount = profitDetail.Weight.Mul(releasedProfitsInformation.ProfitsAmount)
                                .Div(releasedProfitsInformation.TotalWeight)
                        });
                    }
                }
            }

            return new Empty();
        }

        public override CreatedProfitItems GetCreatedProfitItems(GetCreatedProfitItemsInput input)
        {
            return State.CreatedProfitItemsMap[input.Creator];
        }

        public override ProfitItem GetProfitItem(Hash input)
        {
            return State.ProfitItemsMap[input];
        }

        public override Address GetProfitItemVirtualAddress(GetProfitItemVirtualAddressInput input)
        {
            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(input.ProfitId);
            return input.Period == 0
                ? virtualAddress
                : GetReleasedPeriodProfitsVirtualAddress(virtualAddress, input.Period);
        }

        public override ReleasedProfitsInformation GetReleasedProfitsInformation(GetReleasedProfitsInformationInput input)
        {
            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(input.ProfitId);
            var releasedProfitsVirtualAddress = GetReleasedPeriodProfitsVirtualAddress(virtualAddress, input.Period);
            return State.ReleasedProfitsMap[releasedProfitsVirtualAddress];
        }

        private Address GetReleasedPeriodProfitsVirtualAddress(Address profitId, long period)
        {
            return Address.FromPublicKey(period.ToString().CalculateHash().Concat(profitId.Value).ToArray());
        }
    }
}