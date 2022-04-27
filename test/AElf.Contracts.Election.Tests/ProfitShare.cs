using System.Collections.Generic;
using System.Linq;

namespace AElf.Contracts.Election
{
    public class ProfitShare : Dictionary<int, Dictionary<string, long>>
    {
        public Dictionary<string, long> GetSharesOfPeriod(int period)
        {
            TryGetValue(period, out var shareMap);
            return shareMap;
        }

        public long GetTotalSharesOfPeriod(int period)
        {
            TryGetValue(period, out var shareMap);
            return shareMap == null ? 0 : shareMap.Values.Sum();
        }

        public void AddShares(int startPeriod, int endPeriod, string voterPubkey, long shares)
        {
            for (var period = startPeriod; period <= endPeriod; period++)
            {
                if (ContainsKey(period))
                {
                    if (this[period].ContainsKey(voterPubkey))
                    {
                        this[period][voterPubkey] += shares;
                    }
                    else
                    {
                        this[period].Add(voterPubkey, shares);
                    }
                }
                else
                {
                    TryAdd(period, new Dictionary<string, long>
                    {
                        { voterPubkey, shares }
                    });
                }
            }
        }

        public long CalculateProfits(int period, long totalProfits, string voterPubkey)
        {
            var totalShares = GetTotalSharesOfPeriod(period);
            var voterShares = GetSharesOfPeriod(period)[voterPubkey];
            return voterShares / totalShares * totalProfits;
        }
    }
}