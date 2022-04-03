using System.Collections.Generic;
using AElf.Types;

namespace AElf.Contracts.Profit.Services
{
    public interface IProfitService
    {
        void Contribute(Hash schemeId, long period, string symbol, long amount);
        void Distribute(Hash schemeId, long period, Dictionary<string, long> amountMap);
        void Claim(Hash schemeId, Address beneficiary);
        void Burn(Hash schemeId, long period, Dictionary<string, long> amountMap);
    }
}