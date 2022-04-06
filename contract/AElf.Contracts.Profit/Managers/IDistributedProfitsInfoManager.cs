using System.Collections.Generic;
using AElf.Types;

namespace AElf.Contracts.Profit.Managers
{
    public interface IDistributedProfitsInfoManager
    {
        void AddProfits(Hash schemeId, long period, string symbol, long amount);
        void MarkAsDistributed(Hash schemeId, long period, long totalShares, Dictionary<string, long> actualAmountMap);

        DistributedProfitsInfo GetDistributedProfitsInfo(Hash schemeId, long period);
    }
}