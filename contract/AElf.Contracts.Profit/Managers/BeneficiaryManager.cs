using System.Linq;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Profit.Managers
{
    public class BeneficiaryManager : IBeneficiaryManager
    {
        private readonly CSharpSmartContractContext _context;
        private readonly IProfitSchemeManager _profitSchemeManager;
        private readonly IProfitDetailManager _profitDetailManager;

        public BeneficiaryManager(CSharpSmartContractContext context,
            IProfitSchemeManager profitSchemeManager, IProfitDetailManager profitDetailManager)
        {
            _context = context;
            _profitSchemeManager = profitSchemeManager;
            _profitDetailManager = profitDetailManager;
        }

        public void AddBeneficiary(Hash schemeId, BeneficiaryShare beneficiaryShare, long endPeriod,
            long startPeriod = 0, Hash profitDetailId = null, bool isFixProfitDetail = false)
        {
            if (schemeId == null)
            {
                throw new AssertionException("Invalid scheme id.");
            }

            if (beneficiaryShare?.Beneficiary == null)
            {
                throw new AssertionException("Invalid beneficiary address.");
            }

            if (beneficiaryShare.Shares < 0)
            {
                throw new AssertionException("Invalid share.");
            }

            if (endPeriod == 0)
            {
                // Which means this profit Beneficiary will never expired unless removed.
                endPeriod = long.MaxValue;
            }

            var scheme = _profitSchemeManager.GetScheme(schemeId);

            if (_context.Sender != scheme.Manager && _context.Sender !=
                _context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName))
            {
                throw new AssertionException("Only manager or token holder contract can add beneficiary.");
            }

            _context.LogDebug(() =>
                $"{schemeId}.\n End Period: {endPeriod}, Current Period: {scheme.CurrentPeriod}");

            if (endPeriod < scheme.CurrentPeriod)
            {
                throw new AssertionException(
                    $"Invalid end period. End Period: {endPeriod}, Current Period: {scheme.CurrentPeriod}");
            }

            if (!isFixProfitDetail)
            {
                _profitSchemeManager.AddShares(schemeId, beneficiaryShare.Shares);
            }

            if (startPeriod == 0)
            {
                startPeriod = scheme.CurrentPeriod.Add(scheme.DelayDistributePeriodCount);
            }

            _profitDetailManager.AddProfitDetail(schemeId, beneficiaryShare.Beneficiary, new ProfitDetail
            {
                StartPeriod = startPeriod,
                EndPeriod = endPeriod,
                Shares = beneficiaryShare.Shares,
                Id = profitDetailId
            });
        }

        public void RemoveBeneficiary(Hash schemeId, Address beneficiary, Hash profitDetailId = null, bool isSubScheme = false)
        {
            if (schemeId == null)
            {
                throw new AssertionException("Invalid scheme id.");
            }

            if (beneficiary == null)
            {
                throw new AssertionException("Invalid Beneficiary address.");
            }

            var scheme = _profitSchemeManager.GetScheme(schemeId);

            if (_context.Sender != scheme.Manager && _context.Sender !=
                _context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName))
            {
                throw new AssertionException("Only manager or token holder contract can add beneficiary.");
            }

            var removedDetails =
                _profitDetailManager.RemoveProfitDetails(scheme, beneficiary, profitDetailId, isSubScheme);

            foreach (var removedDetail in removedDetails)
            {
                if (scheme.DelayDistributePeriodCount > 0)
                {
                    var removedMinPeriod = removedDetail.Key;
                    var removedShares = removedDetail.Value;
                    for (var removedPeriod = removedMinPeriod;
                         removedPeriod < removedMinPeriod.Add(scheme.DelayDistributePeriodCount);
                         removedPeriod++)
                    {
                        if (scheme.CachedDelayTotalShares.ContainsKey(removedPeriod))
                        {
                            scheme.CachedDelayTotalShares[removedPeriod] =
                                scheme.CachedDelayTotalShares[removedPeriod].Sub(removedShares);
                        }
                    }
                }
            }

            _profitSchemeManager.RemoveShares(schemeId, removedDetails.Values.Sum());
        }
    }
}