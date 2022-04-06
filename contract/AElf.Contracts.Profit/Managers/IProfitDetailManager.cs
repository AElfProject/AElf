using AElf.Types;

namespace AElf.Contracts.Profit.Managers
{
    public interface IProfitDetailManager
    {
        void AddProfitDetail(Hash schemeId, Address beneficiary, ProfitDetail profitDetail);
        void UpdateProfitDetailLastProfitPeriod(Hash schemeId, Address subSchemeVirtualAddress, long updateTo);
        void ClearProfitDetails(Hash schemeId, Address beneficiary);
        long RemoveProfitDetails(Scheme scheme, Address beneficiary, bool isSubScheme = false);
        long RemoveClaimedProfitDetails(Hash schemeId, Address beneficiary);
        ProfitDetails GetProfitDetails(Hash schemeId, Address beneficiary);
    }
}