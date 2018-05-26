using System.Collections.Generic;
using AElf.Kernel.Concurrency;

namespace AElf.Kernel.Tests.Concurrency.Metadata
{
    
    public class TokenTransfer
    {
        [SmartContractFieldData("BalanceMap", DataAccessMode.AccountSpecific)]
        private readonly Dictionary<Hash, long> _balanceMap;
        
        [SmartContractFieldData("TokenContractName", DataAccessMode.ReadOnlyAccountSharing)]
        public string TokenContractName { get; }

        TokenTransfer(string tokenContractName, List<Hash> playerAddressList)
        {
            TokenContractName = tokenContractName;
            _balanceMap = new Dictionary<Hash, long>();
            playerAddressList.ForEach(user => _balanceMap.Add(user, 10000));
        }

        
        public string GetName()
        {
            return TokenContractName;
        }
        
        [SmartContractFunction("Transfer(Hash, Hash, long)")]
        public bool Transfer(Hash sender, Hash receiver, long value)
        {
            //TODO: considering safety, this sender need to get from somewhere else 
            if (value > _balanceMap[sender])
            {
                return false;
            }

            _balanceMap[sender] -= value;
            _balanceMap[receiver] += value;
            return true;
        }
    }
}