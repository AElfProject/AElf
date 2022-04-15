using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Profit.Managers
{
    public class ProfitDetailManager : IProfitDetailManager
    {
        private readonly CSharpSmartContractContext _context;
        private readonly MappedState<Hash, Address, ProfitDetails> _profitDetailsMap;

        public ProfitDetailManager(CSharpSmartContractContext context,
            MappedState<Hash, Address, ProfitDetails> profitDetailsMap)
        {
            _profitDetailsMap = profitDetailsMap;
            _context = context;
        }

        public void AddProfitDetail(Hash schemeId, Address beneficiary, ProfitDetail profitDetail)
        {
            if (_profitDetailsMap[schemeId][beneficiary] == null)
            {
                _profitDetailsMap[schemeId][beneficiary] = new ProfitDetails
                {
                    Details = { profitDetail }
                };
            }
            else
            {
                _profitDetailsMap[schemeId][beneficiary].Details.Add(profitDetail);
            }

            _context.LogDebug(() =>
                $"Added {profitDetail.Shares} weights to scheme {schemeId.ToHex()}: {profitDetail}");

            _context.Fire(new ProfitDetailAdded
            {
                Beneficiary = beneficiary,
                Shares = profitDetail.Shares,
                StartPeriod = profitDetail.StartPeriod,
                EndPeriod = profitDetail.EndPeriod,
                IsWeightRemoved = profitDetail.IsWeightRemoved
            });
        }

        public void UpdateProfitDetailLastProfitPeriod(Hash schemeId, Address subSchemeVirtualAddress, long updateTo)
        {
            var subSchemeDetails = _profitDetailsMap[schemeId][subSchemeVirtualAddress];
            foreach (var detail in subSchemeDetails.Details)
            {
                detail.LastProfitPeriod = updateTo;
            }

            _profitDetailsMap[schemeId][subSchemeVirtualAddress] = subSchemeDetails;
        }

        public void ClearProfitDetails(Hash schemeId, Address beneficiary)
        {
            _profitDetailsMap[schemeId].Remove(beneficiary);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scheme"></param>
        /// <param name="beneficiary"></param>
        /// <param name="isSubScheme"></param>
        /// <returns>Removed Shares</returns>
        public long RemoveProfitDetails(Scheme scheme, Address beneficiary, bool isSubScheme = false)
        {
            var removedShares = 0L;

            var profitDetails = _profitDetailsMap[scheme.SchemeId][beneficiary];
            if (profitDetails == null)
            {
                return 0;
            }

            List<ProfitDetail> detailsCanBeRemoved;

            if (isSubScheme)
            {
                detailsCanBeRemoved = profitDetails.Details.ToList();
            }
            else
            {
                detailsCanBeRemoved = scheme.CanRemoveBeneficiaryDirectly
                    ? profitDetails.Details.Where(d => !d.IsWeightRemoved).ToList()
                    : profitDetails.Details
                        .Where(d => d.EndPeriod < scheme.CurrentPeriod && !d.IsWeightRemoved).ToList();
            }

            if (!detailsCanBeRemoved.Any())
            {
                return removedShares;
            }

            removedShares = detailsCanBeRemoved.Sum(d => d.Shares);
            foreach (var profitDetail in detailsCanBeRemoved)
            {
                profitDetail.IsWeightRemoved = true;
                if (profitDetail.LastProfitPeriod >= scheme.CurrentPeriod)
                {
                    profitDetails.Details.Remove(profitDetail);
                }
                else if (profitDetail.EndPeriod >= scheme.CurrentPeriod)
                {
                    profitDetail.EndPeriod = scheme.CurrentPeriod.Sub(1);
                }
            }

            _context.LogDebug(() => $"ProfitDetails after removing expired details: {profitDetails}");

            // Clear old profit details.
            if (profitDetails.Details.Count != 0)
            {
                _profitDetailsMap[scheme.SchemeId][beneficiary] = profitDetails;
            }
            else
            {
                ClearProfitDetails(scheme.SchemeId, beneficiary);
            }

            return removedShares;
        }

        public long RemoveClaimedProfitDetails(Hash schemeId, Address beneficiary)
        {
            var profitDetails = _profitDetailsMap[schemeId][beneficiary];
            var detailsCanBeRemoved = profitDetails.Details.Where(d => d.EndPeriod == d.LastProfitPeriod).ToList();
            foreach (var profitDetail in detailsCanBeRemoved)
            {
                _profitDetailsMap[schemeId][beneficiary].Details.Remove(profitDetail);
            }

            return detailsCanBeRemoved.Sum(d => d.Shares);
        }

        public void FixProfitDetail(Hash schemeId, BeneficiaryShare beneficiaryShare, long startPeriod, long endPeriod)
        {
            var profitDetails = _profitDetailsMap[schemeId][beneficiaryShare.Beneficiary];
            var fixingDetail = profitDetails.Details.OrderBy(d => d.StartPeriod)
                .FirstOrDefault(d => d.Shares == beneficiaryShare.Shares);
            if (fixingDetail == null)
            {
                throw new AssertionException("Cannot find proper profit detail to fix.");
            }

            var newDetail = fixingDetail.Clone();
            newDetail.StartPeriod = startPeriod == 0 ? fixingDetail.StartPeriod : startPeriod;
            newDetail.EndPeriod = endPeriod == 0 ? fixingDetail.EndPeriod : endPeriod;
            profitDetails.Details.Remove(fixingDetail);
            profitDetails.Details.Add(newDetail);
        }

        public ProfitDetails GetProfitDetails(Hash schemeId, Address beneficiary)
        {
            return _profitDetailsMap[schemeId][beneficiary];
        }
    }
}