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
        public override CreatedProfitIds GetCreatedProfitIds(GetCreatedProfitIdsInput input)
        {
            return State.CreatedProfitIds[input.Creator];
        }

        public override ProfitItem GetProfitItem(Hash input)
        {
            return State.ProfitItemsMap[input];
        }

        /// <summary>
        /// If input.Period == 0, the result will be the address of general ledger of a certain profit item;
        /// Otherwise the result will be the address of a specific account period of a certain profit item,
        /// which profit receivers will gain profits from.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
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
            {
                TotalWeight = -1
            };
        }

        public override ProfitDetails GetProfitDetails(GetProfitDetailsInput input)
        {
            return State.ProfitDetailsMap[input.ProfitId][input.Receiver];
        }

        private Address GetReleasedPeriodProfitsVirtualAddress(Address profitId, long period)
        {
            return Address.FromPublicKey(period.ToString().CalculateHash().Concat(profitId.Value).ToArray());
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

            var availableDetails = profitDetails.Details.Where(d =>
                d.LastProfitPeriod < profitItem.CurrentPeriod && d.EndPeriod >= d.LastProfitPeriod
            ).ToList();

            var amount = 0L;

            for (var i = 0;
                i < Math.Min(ProfitContractConsts.ProfitReceivingLimitForEachTime, availableDetails.Count);
                i++)
            {
                var profitDetail = availableDetails[i];
                if (profitDetail.LastProfitPeriod == 0)
                {
                    profitDetail.LastProfitPeriod = profitDetail.StartPeriod;
                }

                amount = amount.Add(ProfitAllPeriods(profitItem, input.Symbol, profitDetail, profitVirtualAddress, true));
            }

            return new SInt64Value {Value = amount};
        }
    }
}