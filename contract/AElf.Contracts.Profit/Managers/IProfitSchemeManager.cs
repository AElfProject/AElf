using AElf.Types;

namespace AElf.Contracts.Profit.Managers
{
    public interface IProfitSchemeManager
    {
        void CreateNewScheme(Scheme scheme);
        void AddSubScheme(Hash schemeId, Hash subSchemeId, long shares);
        void RemoveSubScheme(Hash schemeId, Hash subSchemeId);
        void AddShares(Hash schemeId, long shares);
        void RemoveShares(Hash schemeId, long shares);
        void AddReceivedTokenSymbol(Hash schemeId, string symbol);
        void MoveToNextPeriod(Hash schemeId);
        
        Scheme GetScheme(Hash schemeId);
        void CheckSchemeExists(Hash schemeId);
    }
}