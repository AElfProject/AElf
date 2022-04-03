using AElf.Types;

namespace AElf.Contracts.Profit.Managers
{
    public interface IProfitDetailManager
    {
        void AddProfitDetail(Hash schemeId, Address beneficiary, ProfitDetail profitDetail);
        void ClearProfitDetails(Hash schemeId, Address beneficiary);
        long RemoveProfitDetails(Scheme scheme, Address beneficiary);
    }
}