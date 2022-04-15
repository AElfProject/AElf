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
            long startPeriod = 0, bool isFixProfitDetail = false)
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
            });
        }

        public void RemoveBeneficiary(Hash schemeId, Address beneficiary, bool isSubScheme = false)
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

            var removedShares = _profitDetailManager.RemoveProfitDetails(scheme, beneficiary, isSubScheme);
            _profitSchemeManager.RemoveShares(schemeId, removedShares);
        }
    }
}