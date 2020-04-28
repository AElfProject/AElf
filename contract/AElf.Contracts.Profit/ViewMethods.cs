using System;
using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Types;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

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
        /// If input.Period == 0, the result will be the address of general ledger of a certain profit scheme;
        /// Otherwise the result will be the address of a specific account period of a certain profit scheme,
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
            return Address.FromPublicKey(HashHelper.ComputeFrom(period).ToByteArray().Concat(profitId.Value).ToArray());
        }

        public override Int64Value GetProfitAmount(GetProfitAmountInput input)
        {
            var profitItem = State.SchemeInfos[input.SchemeId];
            Assert(profitItem != null, "Scheme not found.");
            var beneficiary = input.Beneficiary ?? Context.Sender;
            var profitDetails = State.ProfitDetailsMap[input.SchemeId][beneficiary];

            if (profitDetails == null)
            {
                return new Int64Value {Value = 0};
            }

            var profitVirtualAddress = Context.ConvertVirtualAddressToContractAddress(input.SchemeId);

            // ReSharper disable once PossibleNullReferenceException
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

                var profitsDict = ProfitAllPeriods(profitItem, profitDetail, profitVirtualAddress, beneficiary, true,
                    input.Symbol);
                amount = amount.Add(profitsDict[input.Symbol]);
            }

            return new Int64Value {Value = amount};
        }

        public override ReceivedProfitsMap GetProfitsMap(ClaimProfitsInput input)
        {
            var scheme = State.SchemeInfos[input.SchemeId];
            Assert(scheme != null, "Scheme not found.");
            var beneficiary = input.Beneficiary ?? Context.Sender;
            var profitDetails = State.ProfitDetailsMap[input.SchemeId][beneficiary];

            if (profitDetails == null)
            {
                return new ReceivedProfitsMap();
            }

            var profitVirtualAddress = Context.ConvertVirtualAddressToContractAddress(input.SchemeId);

            // ReSharper disable once PossibleNullReferenceException
            var availableDetails = profitDetails.Details.Where(d =>
                d.LastProfitPeriod < scheme.CurrentPeriod && d.EndPeriod >= d.LastProfitPeriod
            ).ToList();

            var profitsDict = new Dictionary<string, long>();
            for (var i = 0;
                i < Math.Min(ProfitContractConstants.ProfitReceivingLimitForEachTime, availableDetails.Count);
                i++)
            {
                var profitDetail = availableDetails[i];
                if (profitDetail.LastProfitPeriod == 0)
                {
                    profitDetail.LastProfitPeriod = profitDetail.StartPeriod;
                }

                profitsDict = ProfitAllPeriods(scheme, profitDetail, profitVirtualAddress, beneficiary, true);
            }

            return new ReceivedProfitsMap
            {
                Value = {profitsDict}
            };
        }
    }
}