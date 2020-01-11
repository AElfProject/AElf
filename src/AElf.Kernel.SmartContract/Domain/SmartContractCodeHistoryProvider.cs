using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Domain
{
    public interface ISmartContractCodeHistoryProvider
    {
        SmartContractCodeHistory GetSmartContractCodeHistory(Address address);
        void SetSmartContractCodeHistory(Address address,SmartContractCodeHistory smartContractCodeHistory);
        void AddSmartContractCode(Address address, Hash codeHash, BlockIndex blockIndex);
        Dictionary<Address,List<SmartContractCode>> Remove(List<BlockIndex> blockIndexes);
    }

    public class SmartContractCodeHistoryProvider : ISmartContractCodeHistoryProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<Address, SmartContractCodeHistory>
            _addressSmartContractCodeHistoryMappingCache =
                new ConcurrentDictionary<Address, SmartContractCodeHistory>();

        public SmartContractCodeHistory GetSmartContractCodeHistory(Address address)
        {
            _addressSmartContractCodeHistoryMappingCache.TryGetValue(address, out var codeHistory);
            return codeHistory;
        }
        
        public void SetSmartContractCodeHistory(Address address, SmartContractCodeHistory smartContractCodeHistory)
        {
            _addressSmartContractCodeHistoryMappingCache[address] = smartContractCodeHistory;
        }

        public void AddSmartContractCode(Address address, Hash codeHash, BlockIndex blockIndex)
        {
            if(!_addressSmartContractCodeHistoryMappingCache.TryGetValue(address,out var codeHistory))
            {
                codeHistory = new SmartContractCodeHistory();
                _addressSmartContractCodeHistoryMappingCache[address] = codeHistory;
            }

            codeHistory.Codes.Add(new SmartContractCode
            {
                BlockHash = blockIndex.BlockHash,
                BlockHeight = blockIndex.BlockHeight,
                CodeHash = codeHash
            });
        }
        
        public Dictionary<Address,List<SmartContractCode>> Remove(List<BlockIndex> blockIndexes)
        {
            var smartContractCodeDic = new Dictionary<Address,List<SmartContractCode>>();
            var addresses = _addressSmartContractCodeHistoryMappingCache.Keys.ToList();
            var blockHashes = blockIndexes.Select(b => b.BlockHash).ToList();
            foreach (var address in addresses)
            {
                var smartContractCodeHistory = _addressSmartContractCodeHistoryMappingCache[address];
                smartContractCodeDic[address] = smartContractCodeHistory.Codes
                    .Where(code => blockHashes.Contains(code.BlockHash)).ToList();
;                smartContractCodeHistory.Codes.RemoveAll(code => blockHashes.Contains(code.BlockHash));
                if (smartContractCodeHistory.Codes.Count != 0) continue;
                _addressSmartContractCodeHistoryMappingCache.TryRemove(address, out _);
            }

            return smartContractCodeDic;
        }
    }
}