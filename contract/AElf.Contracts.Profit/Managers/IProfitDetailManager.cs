using AElf.Contracts.Profit.Models;
using AElf.Types;

namespace AElf.Contracts.Profit.Managers
{
    public interface IProfitDetailManager
    {
        void AddProfitDetail(Hash schemeId, Address beneficiary, ProfitDetail profitDetail);
        void UpdateProfitDetailLastProfitPeriod(Hash schemeId, Address subSchemeVirtualAddress, long updateTo);
        void ClearProfitDetails(Hash schemeId, Address beneficiary);
        RemovedDetails RemoveProfitDetails(Scheme scheme, Address beneficiary, bool isSubScheme = false);
        long RemoveClaimedProfitDetails(Hash schemeId, Address beneficiary);
        void FixProfitDetail(Hash schemeId, BeneficiaryShare beneficiaryShare, long startPeriod, long endPeriod);
        ProfitDetails GetProfitDetails(Hash schemeId, Address beneficiary);
    }
}