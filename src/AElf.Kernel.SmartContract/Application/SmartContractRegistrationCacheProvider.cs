using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractRegistrationCacheProvider
    {
        Task<SmartContractRegistration> GetSmartContractRegistrationAsync(IChainContext chainContext,
            Address address);
        bool TryGetValue(Address address, out SmartContractRegistration smartContractRegistration);
        void Set(Address address, SmartContractRegistration smartContractRegistration);
    }
    
    
    
    //TODO: why not directly user ConcurrentDictionary? There is no meaning to make a new provider.
    //If you want to cache something from BlockchainStateService.GetBlockExecutedDataAsync, it makes sense.
    //But your implement in here is completely no meaning.
    public class SmartContractRegistrationCacheProvider : ISmartContractRegistrationCacheProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<Address, SmartContractRegistration> _smartContractRegistrationCache =
            new ConcurrentDictionary<Address, SmartContractRegistration>();

        private readonly ISmartContractCodeHashProvider _smartContractCodeHashProvider;
        private readonly ISmartContractChangeHeightProvider _smartContractChangeHeightProvider;

        public SmartContractRegistrationCacheProvider(ISmartContractCodeHashProvider smartContractCodeHashProvider,
            ISmartContractChangeHeightProvider smartContractChangeHeightProvider)
        {
            _smartContractCodeHashProvider = smartContractCodeHashProvider;
            _smartContractChangeHeightProvider = smartContractChangeHeightProvider;
        }

        public bool TryGetValue(Address address, out SmartContractRegistration smartContractRegistration)
        {
            return _smartContractRegistrationCache.TryGetValue(address, out smartContractRegistration);
        }

        public async Task<SmartContractRegistration> GetSmartContractRegistrationAsync(IChainContext chainContext,
            Address address)
        {
            if (_smartContractRegistrationCache.TryGetValue(address, out var smartContractRegistration))
            {
                if (!_smartContractChangeHeightProvider.TryGetValue(address, out var blockHeight))
                    return smartContractRegistration;

                //if contract has smartContractRegistration cache and update height. we need to get code hash in block
                //executed cache to check whether it is equal to the one in cache.
                var codeHash =
                    await _smartContractCodeHashProvider.GetSmartContractCodeHashAsync(chainContext, address);
                if (smartContractRegistration.CodeHash != codeHash || blockHeight == chainContext.BlockHeight + 1)
                {
                    //registration is null or registration's code hash isn't equal to cache's code hash
                    //or current height is equal to update height.maybe the cache is wrong. so we return null
                    return null;
                }
            }

            return smartContractRegistration;
        }

        public void Set(Address address, SmartContractRegistration smartContractRegistration)
        {
            _smartContractRegistrationCache[address] = smartContractRegistration;
        }
    }
}