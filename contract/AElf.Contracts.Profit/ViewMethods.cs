using System;
using System.Linq;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using AElf.Types;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContract
    {
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

        public override ReleasedProfitsInformation GetReleasedProfitsInformation(
            GetReleasedProfitsInformationInput input)
        {
            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(input.ProfitId);
            var releasedProfitsVirtualAddress = GetReleasedPeriodProfitsVirtualAddress(virtualAddress, input.Period);
            return State.ReleasedProfitsMap[releasedProfitsVirtualAddress] ?? new ReleasedProfitsInformation
                       {ProfitsAmount = -1, TotalWeight = -1};
        }

        public override ProfitDetails GetProfitDetails(GetProfitDetailsInput input)
        {
            return State.ProfitDetailsMap[input.ProfitId][input.Receiver];
        }

        private Address GetReleasedPeriodProfitsVirtualAddress(Address profitId, long period)
        {
            return Address.FromPublicKey(period.ToString().ComputeHash().Concat(profitId.Value).ToArray());
        }

        public override SInt64Value GetProfitAmount(ProfitInput input)
        {
            var profitItem = State.ProfitItemsMap[input.ProfitId];
            Assert(profitItem != null, "Profit item not found.");

            var profitDetails = State.ProfitDetailsMap[input.ProfitId][Context.Sender];

            Assert(profitDetails != null, "Profit details not found.");

            if (profitDetails == null || profitItem == null)
            {
                return new SInt64Value {Value = 0};
            }

            var profitVirtualAddress = Context.ConvertVirtualAddressToContractAddress(input.ProfitId);

            var availableDetails = profitDetails.Details.Where(d => d.LastProfitPeriod != profitItem.CurrentPeriod)
                .ToList();

            var amount = 0L;

            for (var i = 0; i < Math.Min(ProfitContractConsts.ProfitLimit, availableDetails.Count); i++)
            {
                var profitDetail = availableDetails[i];
                if (profitDetail.LastProfitPeriod == 0)
                {
                    profitDetail.LastProfitPeriod = profitDetail.StartPeriod;
                }

                for (var period = profitDetail.LastProfitPeriod;
                    period <= (profitDetail.EndPeriod == long.MaxValue
                        ? profitItem.CurrentPeriod - 1
                        : Math.Min(profitItem.CurrentPeriod - 1, profitDetail.EndPeriod));
                    period++)
                {
                    var releasedProfitsVirtualAddress =
                        GetReleasedPeriodProfitsVirtualAddress(profitVirtualAddress, period);
                    var releasedProfitsInformation = State.ReleasedProfitsMap[releasedProfitsVirtualAddress];
                    if (releasedProfitsInformation.IsReleased)
                    {
                        amount = amount.Add(profitDetail.Weight.Mul(releasedProfitsInformation.ProfitsAmount)
                            .Div(releasedProfitsInformation.TotalWeight));
                    }
                }
            }

            return new SInt64Value {Value = amount};
        }
    }
}