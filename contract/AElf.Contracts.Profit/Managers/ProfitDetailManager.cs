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
            var detailsCanBeRemoved = new List<ProfitDetail>();

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
    }
}