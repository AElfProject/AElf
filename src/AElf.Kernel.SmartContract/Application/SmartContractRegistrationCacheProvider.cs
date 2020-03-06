using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractRegistrationCacheProvider
    {
        bool TryGetValue(Address address, out SmartContractRegistration smartContractRegistration);
        bool TryAdd(Address address, SmartContractRegistration smartContractRegistration);
        void Set(Address address, SmartContractRegistration smartContractRegistration);
        bool TryRemove(Address address, out SmartContractRegistration smartContractRegistration);
    }
    
    
    
    //TODO: why not directly user ConcurrentDictionary? There is no meaning to make a new provider.
    //If you want to cache something from BlockchainStateService.GetBlockExecutedDataAsync, it makes sense.
    //But your implement in here is completely no meaning.
    public class SmartContractRegistrationCacheProvider : ISmartContractRegistrationCacheProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<Address, SmartContractRegistration> _smartContractRegistrationCache =
            new ConcurrentDictionary<Address, SmartContractRegistration>();
        
        public bool TryGetValue(Address address, out SmartContractRegistration smartContractRegistration)
        {
            return _smartContractRegistrationCache.TryGetValue(address, out smartContractRegistration);
        }

        public bool TryAdd(Address address, SmartContractRegistration smartContractRegistration)
        {
            return _smartContractRegistrationCache.TryAdd(address, smartContractRegistration);
        }

        public void Set(Address address, SmartContractRegistration smartContractRegistration)
        {
            _smartContractRegistrationCache[address] = smartContractRegistration;
        }

        public bool TryRemove(Address address, out SmartContractRegistration smartContractRegistration)
        {
            return _smartContractRegistrationCache.TryRemove(address, out smartContractRegistration);
        }
    }
}