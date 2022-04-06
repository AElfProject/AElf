using System.Collections.Generic;
using AElf.Contracts.Profit.Helpers;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Profit.Managers
{
    public class DistributedProfitsInfoManager : IDistributedProfitsInfoManager
    {
        private readonly CSharpSmartContractContext _context;
        private readonly MappedState<Address, DistributedProfitsInfo> _distributedProfitsInfoMap;

        public DistributedProfitsInfoManager(CSharpSmartContractContext context,
            MappedState<Address, DistributedProfitsInfo> distributedProfitsInfoMap)
        {
            _context = context;
            _distributedProfitsInfoMap = distributedProfitsInfoMap;
        }

        public void AddProfits(Hash schemeId, long period, string symbol, long amount)
        {
            var periodVirtualAddress = ProfitHelper.CalculatePeriodVirtualAddress(_context, schemeId, period);
            var distributedProfitsInfo = _distributedProfitsInfoMap[periodVirtualAddress];
            if (distributedProfitsInfo == null)
            {
                distributedProfitsInfo = new DistributedProfitsInfo
                {
                    AmountsMap = { { symbol, amount } }
                };
            }
            else
            {
                if (distributedProfitsInfo.IsReleased)
                {
                    throw new AssertionException($"Scheme of period {period} already released.");
                }

                distributedProfitsInfo.AmountsMap[symbol] =
                    distributedProfitsInfo.AmountsMap[symbol].Add(amount);
            }

            _distributedProfitsInfoMap[periodVirtualAddress] = distributedProfitsInfo;
        }

        public void MarkAsDistributed(Hash schemeId, long period, long totalShares,
            Dictionary<string, long> actualAmountMap)
        {
            var periodVirtualAddress = ProfitHelper.CalculatePeriodVirtualAddress(_context, schemeId, period);
            var distributedProfitsInfo = GetDistributedProfitsInfo(schemeId, period);
            distributedProfitsInfo.TotalShares = totalShares;
            distributedProfitsInfo.IsReleased = true;
            foreach (var profits in actualAmountMap)
            {
                var symbol = profits.Key;
                var amount = profits.Value;
                if (distributedProfitsInfo.AmountsMap.ContainsKey(symbol))
                {
                    distributedProfitsInfo.AmountsMap[symbol] = distributedProfitsInfo.AmountsMap[symbol].Add(amount);
                }
                else
                {
                    distributedProfitsInfo.AmountsMap[symbol] = amount;
                }
            }

            _distributedProfitsInfoMap[periodVirtualAddress] = distributedProfitsInfo;
        }

        /// <summary>
        /// Will return new instance if not exists.
        /// </summary>
        /// <param name="schemeId"></param>
        /// <param name="period"></param>
        /// <returns></returns>
        public DistributedProfitsInfo GetDistributedProfitsInfo(Hash schemeId, long period)
        {
            var periodVirtualAddress = ProfitHelper.CalculatePeriodVirtualAddress(_context, schemeId, period);
            return _distributedProfitsInfoMap[periodVirtualAddress] ?? new DistributedProfitsInfo();
        }
    }
}