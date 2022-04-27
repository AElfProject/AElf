using AElf.Types;

namespace AElf.Contracts.Profit.Managers
{
    public interface IProfitSchemeManager
    {
        void CreateNewScheme(Scheme scheme);
        void AddSubScheme(Hash schemeId, Hash subSchemeId, long shares);
        void RemoveSubScheme(Hash schemeId, Hash subSchemeId);
        void AddShares(Hash schemeId, long period, long shares);
        void RemoveShares(Hash schemeId, long period, long shares);
        void AddReceivedTokenSymbol(Hash schemeId, string symbol);
        void MoveToNextPeriod(Hash schemeId);
        void ResetSchemeManager(Hash schemeId, Address newManager);
        void CacheDistributedPeriodTotalShares(Hash schemeId, long period, long totalShares);

        Scheme GetScheme(Hash schemeId);
        void CheckSchemeExists(Hash schemeId);
        long GetCacheDistributedPeriodTotalShares(Hash schemeId, long period);
    }
}