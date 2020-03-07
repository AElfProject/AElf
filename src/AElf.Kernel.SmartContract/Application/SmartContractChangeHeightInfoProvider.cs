using System.Collections.Concurrent;
using System.Collections.Generic;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractChangeHeightProvider
    {
        void AddSmartContractChangeHeight(Address address, long blockHeight);
        bool TryGetValue(Address address, out long height);
        void ClearSmartContractChangeHeight(long height);
    }
    
    public class SmartContractChangeHeightProvider : ISmartContractChangeHeightProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<Address, long> _smartContractChangeHeightMappings =
            new ConcurrentDictionary<Address, long>();
        
        public bool TryGetValue(Address address, out long height)
        {
            return _smartContractChangeHeightMappings.TryGetValue(address, out height);
        }
        
        public void AddSmartContractChangeHeight(Address address, long blockHeight)
        {
            if (blockHeight <= Constants.GenesisBlockHeight) return;
            if (!_smartContractChangeHeightMappings.TryGetValue(address, out var height) || blockHeight > height)
                _smartContractChangeHeightMappings[address] = blockHeight;
        }
        
        public void ClearSmartContractChangeHeight(long height)
        {
            var removeKeys = new List<Address>();
            foreach (var contractInfo in _smartContractChangeHeightMappings)
            {
                if (contractInfo.Value <= height) removeKeys.Add(contractInfo.Key);
            }

            foreach (var key in removeKeys)
            {
                _smartContractChangeHeightMappings.TryRemove(key, out _);
            }
        }

        
    }
}