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

        public void AddBeneficiary(Hash schemeId, BeneficiaryShare beneficiaryShare, long endPeriod)
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
                throw new AssertionException("Only manager can add beneficiary.");
            }

            _context.LogDebug(() =>
                $"{schemeId}.\n End Period: {endPeriod}, Current Period: {scheme.CurrentPeriod}");

            if (endPeriod < scheme.CurrentPeriod)
            {
                throw new AssertionException(
                    $"Invalid end period. End Period: {endPeriod}, Current Period: {scheme.CurrentPeriod}");
            }

            _profitSchemeManager.AddShares(schemeId, beneficiaryShare.Shares);
            _profitDetailManager.AddProfitDetail(schemeId, beneficiaryShare.Beneficiary, new ProfitDetail
            {
                StartPeriod = scheme.CurrentPeriod.Add(scheme.DelayDistributePeriodCount),
                EndPeriod = endPeriod,
                Shares = beneficiaryShare.Shares,
            });
        }

        public void RemoveBeneficiary(Hash schemeId, Address beneficiary, bool isSubScheme)
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
                throw new AssertionException("Only manager can remove beneficiary.");
            }

            var removedShares = _profitDetailManager.RemoveProfitDetails(scheme, beneficiary, true);
            _profitSchemeManager.RemoveShares(schemeId, removedShares);
        }
    }
}