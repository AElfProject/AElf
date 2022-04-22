using AElf.Types;

namespace AElf.Contracts.Profit.Managers
{
    public interface IBeneficiaryManager
    {
        void AddBeneficiary(Hash schemeId, BeneficiaryShare beneficiaryShare, long endPeriod, long startPeriod = 0,
            Hash profitDetailId = null, bool isFixProfitDetail = false);

        void RemoveBeneficiary(Hash schemeId, Address beneficiary, Hash profitDetailId = null,
            bool isSubScheme = false);
    }
}