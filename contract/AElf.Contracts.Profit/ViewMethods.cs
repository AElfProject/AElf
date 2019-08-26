using System;
using System.Linq;
using AElf.Types;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContract
    {
        public override CreatedSchemeIds GetManagingSchemeIds(GetManagingSchemeIdsInput input)
        {
            return State.ManagingSchemeIds[input.Manager];
        }

        public override Scheme GetScheme(Hash input)
        {
            return State.SchemeInfos[input];
        }

        /// <summary>
        /// If input.Period == 0, the result will be the address of general ledger of a certain profit item;
        /// Otherwise the result will be the address of a specific account period of a certain profit item,
        /// which profit receivers will gain profits from.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Address GetSchemeAddress(SchemePeriod input)
        {
            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(input.SchemeId);
            return input.Period == 0
                ? virtualAddress
                : GetDistributedPeriodProfitsVirtualAddress(virtualAddress, input.Period);
        }

        public override DistributedProfitsInfo GetDistributedProfitsInfo(SchemePeriod input)
        {
            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(input.SchemeId);
            var releasedProfitsVirtualAddress = GetDistributedPeriodProfitsVirtualAddress(virtualAddress, input.Period);
            return State.DistributedProfitsMap[releasedProfitsVirtualAddress] ?? new DistributedProfitsInfo
            {
                TotalShares = -1
            };
        }

        public override ProfitDetails GetProfitDetails(GetProfitDetailsInput input)
        {
            return State.ProfitDetailsMap[input.SchemeId][input.Beneficiary];
        }

        private Address GetDistributedPeriodProfitsVirtualAddress(Address profitId, long period)
        {
            return Address.FromPublicKey(period.ToString().ComputeHash().Concat(profitId.Value).ToArray());
        }

        public override SInt64Value GetProfitAmount(ClaimProfitsInput input)
        {
            var profitItem = State.SchemeInfos[input.SchemeId];
            Assert(profitItem != null, "Scheme not found.");

            var profitDetails = State.ProfitDetailsMap[input.SchemeId][Context.Sender];

            if (profitDetails == null || profitItem == null)
            {
                return new SInt64Value {Value = 0};
            }

            var profitVirtualAddress = Context.ConvertVirtualAddressToContractAddress(input.SchemeId);

            var availableDetails = profitDetails.Details.Where(d =>
                d.LastProfitPeriod < profitItem.CurrentPeriod && d.EndPeriod >= d.LastProfitPeriod
            ).ToList();

            var amount = 0L;

            for (var i = 0;
                i < Math.Min(ProfitContractConstants.ProfitReceivingLimitForEachTime, availableDetails.Count);
                i++)
            {
                var profitDetail = availableDetails[i];
                if (profitDetail.LastProfitPeriod == 0)
                {
                    profitDetail.LastProfitPeriod = profitDetail.StartPeriod;
                }

                amount = amount.Add(
                    ProfitAllPeriods(profitItem, input.Symbol, profitDetail, profitVirtualAddress, true));
            }

            return new SInt64Value {Value = amount};
        }
    }
}