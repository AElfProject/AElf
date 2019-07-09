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
        public override CreatedSchemeIds GetCreatedSchemeIds(GetCreatedSchemeIdsInput input)
        {
            return State.CreatedSchemeIds[input.Creator];
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
                : GetReleasedPeriodProfitsVirtualAddress(virtualAddress, input.Period);
        }

        public override DistributedProfitsInfo GetDistributedProfitsInfo(SchemePeriod input)
        {
            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(input.SchemeId);
            var releasedProfitsVirtualAddress = GetReleasedPeriodProfitsVirtualAddress(virtualAddress, input.Period);
            return State.ReleasedProfitsMap[releasedProfitsVirtualAddress] ?? new DistributedProfitsInfo
            {
                TotalShares = -1
            };
        }

        public override ProfitDetails GetProfitDetails(GetProfitDetailsInput input)
        {
            return State.ProfitDetailsMap[input.SchemeId][input.Beneficiary];
        }

        private Address GetReleasedPeriodProfitsVirtualAddress(Address SchemeId, long period)
        {
            return Address.FromPublicKey(period.ToString().CalculateHash().Concat(SchemeId.Value).ToArray());
        }

        public override SInt64Value GetProfitAmount(ClaimProfitsInput input)
        {
            var profitItem = State.SchemeInfos[input.SchemeId];
            Assert(profitItem != null, "Profit item not found.");

            var profitDetails = State.ProfitDetailsMap[input.SchemeId][Context.Sender];

            Assert(profitDetails != null, "Profit details not found.");

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
                i < Math.Min(ProfitContractConsts.ProfitReceivingLimitForEachTime, availableDetails.Count);
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